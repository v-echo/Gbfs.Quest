using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Gbfs.Quest.Models;

namespace Gbfs.Quest.Data
{
    internal class GbfsProviderMap : ClassMap<GbfsProvider>
    {
        public GbfsProviderMap()
        {
            Map(f => f.CountryCode).Name("Country Code");
            Map(f => f.Name);
            Map(f => f.Location);
            Map(f => f.SystemId).Name("System ID");
            Map(f => f.URL);
            Map(f => f.AutoDiscoveryURL).Name("Auto-Discovery URL");
            Map(f => f.SupportedVersions).Name("Supported Versions").TypeConverter(new ArrayConverter());
            Map(f => f.AuthenticationInfoURL).Name("Authentication Info URL");
        }
    }
}
