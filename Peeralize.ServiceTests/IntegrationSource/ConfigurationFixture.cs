using System;
using Microsoft.Extensions.Configuration;
using nvoid.db.DB.Configuration;

namespace Peeralize.ServiceTests.IntegrationSource
{
    public class ConfigurationFixture : IDisposable
    {

        public ConfigurationFixture()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);
            var config = builder.Build();
            DBConfig.Initialize(config);
        }

        public void Dispose()
        {
        }

    }
}