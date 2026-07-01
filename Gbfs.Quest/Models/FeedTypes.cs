namespace Gbfs.Quest.Models
{
    /// <summary>
    /// A static collection of GBFS feed names, as described by the spec. 
    /// In a real scenario, these could be expanded into descriptors to encapsulate metadata like version etc. or built as factories
    /// </summary>
    public static class FeedTypes
    {
        public const string SystemHours = "system_hours";
        public const string SystemRegions = "system_regions";
        public const string SystemInformation = "system_information";
        public const string StationInformation = "station_information";
        public const string StationStatus = "station_status";
        public const string FreeBikeStatus = "free_bike_status";
        public const string VehicleStatus = "vehicle_status";
        public const string VehicleTypes = "vehicle_types";
        public const string SystemPricingPlans = "system_pricing_plans";
    }
}
