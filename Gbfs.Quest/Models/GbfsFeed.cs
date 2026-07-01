using Gbfs.Quest.Data;
using System.Text.Json.Serialization;

namespace Gbfs.Quest.Models
{
    // Ended up unused, since I went the route of JsonDocument parsing instead of modeling the full feed and metadata specs.
    // Ideally we'd want either a complete, robust domain model (likely given that we have a spec to follow) or a flexible, fault-tolerant document-based model (which would work pretty well to encompass version differences).
    internal record GbfsFeed
    {
        public string Version { get; set; }
        public int Ttl { get; set; }

        [JsonConverter(typeof(UnixEpochConverter))]
        [JsonPropertyName("last_updated")]
        public DateTime LastUpdated { get; set; }
    }
}
