using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nvoid.db.Caching;
using nvoid.db.DB.Configuration;
using Netlyt.Service;
using Netlyt.Service.Data;

namespace Netlyt.ServiceTests
{
    public class DonutConfigurationFixture : IDisposable
    {
        private ManagementDbContext _context;
        public DbContextOptionsBuilder<ManagementDbContext> DbOptionsBuilder { get; private set; }
        public ServiceProvider ServiceProvider { get; set; }

        public DonutConfigurationFixture()
        {
            var p = Process.GetCurrentProcess();
            Debug.WriteLine($"Started test process: {p.Id}");
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);
            var config = builder.Build();
            var postgresConnectionString = config.GetConnectionString("PostgreSQLConnection");
            DbOptionsBuilder = new DbContextOptionsBuilder<ManagementDbContext>()
                .UseNpgsql(postgresConnectionString);
            var services = new ServiceCollection();
            _context = CreateContext();
            DBConfig.Initialize(config);
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
        }


        private void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ManagementDbContext>(s => s.UseInMemoryDatabase("Testing"));
            services.AddTransient<ApiService>(s => new ApiService(_context, null)); 
            services.AddTransient<UserService>(s => new UserService(s.GetService<UserManager<User>>(), s.GetService<ApiService>(), null, null,
                s.GetService<OrganizationService>(), s.GetService<ModelService>(), _context));
            services.AddTransient<ModelService>(s => new ModelService(_context, null));
            services.AddTransient<IntegrationService>(s => new IntegrationService(_context, new ApiService(_context, null), s.GetService<UserService>()));
            services.AddSingleton<RedisCacher>(DBConfig.GetCacheContext());
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