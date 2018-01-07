using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nvoid.db.DB.Configuration;
using Netlyt.Service;
using Netlyt.Service.Data;

namespace Netlyt.ServiceTests.IntegrationSource
{
    public class ConfigurationFixture : IDisposable
    {
        private ManagementDbContext _context;
        public DbContextOptionsBuilder<ManagementDbContext> DbOptionsBuilder { get; private set; }
        public ServiceProvider ServiceProvider { get; set; }

        public ConfigurationFixture()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);
            var config = builder.Build();
            DbOptionsBuilder = new DbContextOptionsBuilder<ManagementDbContext>()
                .UseInMemoryDatabase("Testing");
            var services = new ServiceCollection();
            _context = CreateContext();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
            DBConfig.Initialize(config);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ManagementDbContext>(s => s.UseInMemoryDatabase("Testing"));
            services.AddTransient<ApiService>(s => new ApiService(_context, null));
            services.AddTransient<IntegrationService>(s => new IntegrationService(_context));
        }

        public ManagementDbContext CreateContext()
        {
            return new ManagementDbContext(DbOptionsBuilder.Options);
        }

        public T GetService<T>()
        {
            return ServiceProvider.GetService<T>();
        }

        public void Dispose()
        {

        }

    }
}