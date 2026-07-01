using Gbfs.Quest.Data;
using System.Text.Json.Serialization;

namespace Gbfs.Quest.Models
{
    internal record StationStatus
    {
        [JsonPropertyName("station_id")] 
        public required string StationId { get; set; }

        [JsonPropertyName("num_bikes_available")] 
        public int NumBikesAvailable { get; set; }

        [JsonPropertyName("num_docks_available")] 
        public int? NumDocksAvailable { get; set; }

        [JsonPropertyName("is_installed")]
        [JsonConverter(typeof(IntBoolConverter))]
        public bool IsInstalled { get; set; }

        [JsonPropertyName("is_renting")]
        [JsonConverter(typeof(IntBoolConverter))]
        public bool IsRenting { get; set; }

        [JsonPropertyName("is_returning")]
        [JsonConverter(typeof(IntBoolConverter))]
        public bool IsReturning { get; set; }
    }
}
