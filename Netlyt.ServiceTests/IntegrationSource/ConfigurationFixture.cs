using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using nvoid.db.DB.Configuration;
using Netlyt.Service.Data;

namespace Netlyt.ServiceTests.IntegrationSource
{
    public class ConfigurationFixture : IDisposable
    {
        public DbContextOptionsBuilder<ManagementDbContext> DbOptionsBuilder { get; private set; }
        public ConfigurationFixture()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);
            var config = builder.Build();
            DbOptionsBuilder = new DbContextOptionsBuilder<ManagementDbContext>()
                .UseInMemoryDatabase();

            DBConfig.Initialize(config);
        }

        public ManagementDbContext CreateContext()
        {
            return new ManagementDbContext(DbOptionsBuilder.Options);
        }

        public void Dispose()
        {

        }

    }
}