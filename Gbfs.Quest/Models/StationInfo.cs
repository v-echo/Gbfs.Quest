using Gbfs.Quest.Data;
using System.Text.Json.Serialization;

namespace Gbfs.Quest.Models
{
    internal record StationInfo
    {
        [JsonPropertyName("station_id")] 
        public required string StationId { get; set; }

        [JsonPropertyName("region_id")]
        public string? RegionId { get; set; }
        
        [JsonConverter(typeof(NameArrayConverter))]
        public required string Name { get; set; }

        [JsonPropertyName("short_name")] 
        public string? ShortName { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string? Address { get; set; }

        [JsonPropertyName("post_code")] 
        public string? PostCode { get; set; }

        [JsonPropertyName("rental_methods")]
        public string[]? RentalMethods { get; set; }
        public int? Capacity { get; set; }

        [JsonPropertyName("is_virtual_station")]
        [JsonConverter(typeof(IntBoolConverter))]
        public bool? IsVirtualStation { get; set; }
    }
}
