using Gbfs.Quest.Models;
using System.Text.Json.Serialization;

namespace Gbfs.Quest
{
    [JsonSerializable(typeof(StationInfo[]))]
    [JsonSerializable(typeof(StationStatus[]))]
    [JsonSerializable(typeof(GbfsFeedInfo[]))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }
}
