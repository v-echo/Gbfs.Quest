namespace Gbfs.Quest.Models
{
    internal record GbfsProvider
    {
        public string CountryCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string SystemId { get; set; } = string.Empty;
        public string URL { get; set; } = string.Empty;
        public string AutoDiscoveryURL { get; set; } = string.Empty;
        public string[] SupportedVersions { get; set; } = [];
        public string AuthenticationInfoURL { get; set; } = string.Empty;

        public GbfsProvider()
        {
        }
    }
}
