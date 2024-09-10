using Microsoft.Extensions.Configuration;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Models;

namespace WendlandtVentas.Tests
{
    public class ConfigurationHelper
    {
        public static IConfigurationRoot GetIConfigurationRoot(string outputPath)
        {
            return new ConfigurationBuilder()
                .SetBasePath(outputPath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets("e3dfcccf-0cb3-423a-b302-e3e92e95c128")
                .AddEnvironmentVariables()
                .Build();
        }

        public static OneSignalConfiguration GetApplicationConfiguration(string outputPath)
        {
            var configuration = new OneSignalConfiguration();
            var iConfig = GetIConfigurationRoot(outputPath);
            iConfig.GetSection("OneSignalConfiguration").Bind(configuration);

            return configuration;
        }
    }
}