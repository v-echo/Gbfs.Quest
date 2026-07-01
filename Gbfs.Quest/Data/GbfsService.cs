using CsvHelper;
using Gbfs.Quest.Models;
using Microsoft.Extensions.Caching.Hybrid;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

namespace Gbfs.Quest.Data
{
    /// <summary>
    /// The service class handling the HTTP(S) requests, deserialization and mapping of the GBFS providers and feeds.
    /// </summary>
    /// <param name="factory"></param>
    /// <remarks>
    /// The initial idea was to use the .NET 9+ HybridCache as a key-value store for the GBFS feed data, since it maps pretty well with the TTL on the GBFS spec.
    /// However, this would have involved a recurring loop of HTTP requests depending on the cache expiration, which doesn't fit well with the threading model of this terminal app.
    /// Ultimateiy, this and building the questions as an IAsyncEnumerable stream are a better fit for a web app + real-time connection / messaging broker architecture + front-end. 
    /// Alas, for this assignment I decided to try the CLI route, so sacrifices had to be made.
    /// </remarks>
    internal class GbfsService(IHttpClientFactory factory/*, HybridCache cache*/) : IAsyncDisposable
    {
        // Could also live in a config file or ENV var, or passed as an arg to the app. But let's keep it simple.
        const string ProvidersUrl = "https://raw.githubusercontent.com/MobilityData/gbfs/refs/heads/master/systems.csv";

        GbfsProvider[] Providers { get; set; } = [];
        ConcurrentDictionary<string, (object answer, bool valid)[]> Questions { get; } = new();

        CancellationTokenSource? TokenSource;
        JsonSerializerOptions? SerializerOptions;

        readonly string[] SupportedVersions = ["2.0", "2.1", "2.2", "2.3", "3.0"];
        int QuestionsCount = 0;
        int MaxQuestionsCount = 0;

        /// <summary>
        /// Generate multiple-choice questions from a selection of GBFS providers' feeds.
        /// </summary>
        /// <param name="count">The number of questions to generate</param>
        /// <returns></returns>
        public async Task<KeyValuePair<string, (object answer, bool valid)[]>[]> GetQuestionsAsync(int count)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

            TokenSource = new();

            if (Providers.Length == 0)
                await PopulateProviders(TokenSource.Token);

            if (SerializerOptions is null)
            {
                SerializerOptions = new();
                SerializerOptions.Converters.Add(new UnixEpochConverter());
                SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
                SerializerOptions.PropertyNameCaseInsensitive = true;
            }

            var providers = new Random().GetItems(Providers, 5);
            
            // Of course we could reuse generated questions and avoid re-requesting the data. This is just to keep things fresh every run.
            Questions.Clear();
            QuestionsCount = 0;
            MaxQuestionsCount = count;
            int retries = 0;

            do
            {
                try
                {
                    await Parallel.ForEachAsync(providers, TokenSource.Token, async (feed, token) => await PopulateFeeds(feed, token));
                }
                catch (TaskCanceledException) { }
            }
            while (Questions.IsEmpty && ++retries < 3 && !TokenSource.IsCancellationRequested); // Retry on the occasion that we hit a combo of transient errors and lack of data that results in no questions

            return new Random().GetItems(Questions.ToArray(), count);
        }

        // Get the list of known GBFS providers from the hosted csv and cache it
        private async Task PopulateProviders(CancellationToken token)
        {
            // In this case, we really can't proceed without the providers list, so this is a breaking exception
            var response = await WrapRequest(ProvidersUrl, token) ?? throw new Exception("Could not fetch the list of providers!");            
            var text = await response.Content.ReadAsStringAsync(token);

            using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(token));
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<GbfsProviderMap>();

            // Exclude feeds that require authentication or are only using obsolete (according to the spec) versions. This is strictly to limit the scope of the assignment.
            Providers = [.. csv.GetRecords<GbfsProvider>().Where(f => string.IsNullOrWhiteSpace(f.AuthenticationInfoURL) && f.SupportedVersions.Intersect(SupportedVersions).Any())];
        }
        
        // For a given GBFS provider, request its available feeds through the auto-discovery URL
        private async Task PopulateFeeds(GbfsProvider provider, CancellationToken token)
        {
            var response = await WrapRequest(provider.AutoDiscoveryURL, token);
            if (response is null)
                return; // If one of the providers does not respond in time or its services are unavailable, we don't need to stop the process. Just skip to the next provider.

            // Could avoid parsing the JSON if we assumed the provider will always return the latest version. `if (provider.SupportsV3())` would be more performant at the cost of reliability.
            var json = await response.Content.ReadAsStringAsync(token);
            var feeds = GetFeeds(json);
            if (feeds is null or [])
                return;

            try
            {
                await Parallel.ForEachAsync(feeds, token, async (feed, token) => await GenerateQuestions(feed, token));
            }
            catch (TaskCanceledException) { }
        }

        // For a GBFS feed, get the data and generate the questions based on its type
        private async Task GenerateQuestions(GbfsFeedInfo feed, CancellationToken token)
        {
            var response = await WrapRequest(feed.Url, token);
            if (response is null)
                return; // Same as above. If one of the feeds is unavailable, just skip to the next.

            // Since the scope is time-gated, we'll restrict the output to a few feed types. For a large nr of feeds, the switch would be source generated.
            var questions = feed.Name switch
            {
                FeedTypes.StationInformation => QuestionGenerator.Generate(GetNestedDataArray<StationInfo>(await response.Content.ReadAsStringAsync(token)), token),
                FeedTypes.StationStatus => QuestionGenerator.Generate(GetNestedDataArray<StationStatus>(await response.Content.ReadAsStringAsync(token)), token),
                _ => []
            };

            foreach (var question in questions)
            {
                if (token.IsCancellationRequested)
                    return;

                Questions.TryAdd(question.Key, question.Value);
                Interlocked.Increment(ref QuestionsCount); // We don't use the .Count dictionary property instead because it takes a lock on all keys, incredibly expensive operation

                // The below cancellation pattern cuts the loading time, but also the question diversity. In the end, I leaned towards the latter.
                //if (Volatile.Read(ref QuestionsCount) > MaxQuestionsCount)
                //{
                //    await TokenSource!.CancelAsync();
                //    return;
                //}
            }
        }

        // Parse the array of feeds from a GBFS provider's auto-discovery json
        [SuppressMessage("AOT", "IL3050", Justification = "False Warning: https://github.com/dotnet/runtime/issues/51544#issuecomment-1516232559")]
        [SuppressMessage("Trimming", "IL2026", Justification = "False Warning")]
        GbfsFeedInfo[]? GetFeeds(string json)
        {
            using var doc = JsonDocument.Parse(json);

            if (doc is null)
                return null;

            if (!doc.RootElement.TryGetProperty("version", out var version))
                return null;

            // Check doc version and parse appropriately, return discovery feed array
            if (version.ValueEquals("3.0"))
            {
                return GetNestedDataArray<GbfsFeedInfo>(doc);
            }
            else // 2.x
            {
                return GetLanguageNestedDataArray<GbfsFeedInfo>(doc); // We don't care about the language, only the data
            }
        }

        // Navigate the JSON doc to extract and parse a "feeds"-like array
        [SuppressMessage("AOT", "IL3050", Justification = "False Warning: https://github.com/dotnet/runtime/issues/51544#issuecomment-1516232559")]
        [SuppressMessage("Trimming", "IL2026", Justification = "False Warning")]
        T[] GetNestedDataArray<T>(JsonDocument doc)
        {
            doc.RootElement.TryGetProperty("data", out var data);

            foreach (var property in data.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array) // Should always be true, but just in case
                {
                    var result = property.Value.Deserialize<T[]>(SerializerOptions);
                    return result ?? [];
                }
            }

            return [];
        }

        T[] GetNestedDataArray<T>(string json) => GetNestedDataArray<T>(JsonDocument.Parse(json));

        // Navigate the JSON doc to extract and parse the "feeds" array when there's an intermediate layer, like language
        [SuppressMessage("AOT", "IL3050", Justification = "False Warning: https://github.com/dotnet/runtime/issues/51544#issuecomment-1516232559")]
        [SuppressMessage("Trimming", "IL2026", Justification = "False Warning")]
        T[] GetLanguageNestedDataArray<T>(JsonDocument doc)
        {
            doc.RootElement.TryGetProperty("data", out var data);

            foreach (var language in data.EnumerateObject())
            {
                var property = language.Value.GetProperty("feeds");

                if (property.ValueKind == JsonValueKind.Array) // Should always be true, but just in case
                {
                    var result = property.Deserialize<T[]>(SerializerOptions);
                    return result ?? [];
                }
            }

            return [];
        }

        // Given the high number of concurrent HTTP(S) requests from random providers on multiple versions, both transient and non-transient errors can happen.
        // Some feeds are down, some providers aren't correctly returning responses (according to the spec) so we ignore those in this assignment.
        // Ideally we'd want some kind of retry and backoff logic here, with potential fallback to other versions. However this would introduce additional delay into the UI and it's also a question of time.
        private async Task<HttpResponseMessage?> WrapRequest(string url, CancellationToken token = default)
        {
            try
            {
                var client = factory.CreateClient();
                var response = await client.GetAsync(url, token);
                if (!response.IsSuccessStatusCode)
                    return null;
                return response;
            }
            catch (Exception e) when (e is TaskCanceledException or TimeoutException or HttpRequestException)
            {
                return null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (TokenSource is not null && !TokenSource.IsCancellationRequested)
            {
                await TokenSource.CancelAsync();
                TokenSource.Dispose();
            }
        }
    }
}
