using System;
using System.IO;
using System.Text;
using Donut;
using Donut.Orion;
using EntityFramework.DbContextScope;
using EntityFramework.DbContextScope.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Netlyt.Service;
using Netlyt.Service.Cloud;
using Netlyt.Service.Cloud.Interfaces;
using Netlyt.Service.Data;
using nvoid.db.DB.Configuration;
using Netlyt.Interfaces.Models;

namespace Netlyt.Master
{
    class Program
    {
        public static IConfigurationRoot Configuration { get; set; }
        public static IOrionContext OrionContext { get; private set; }

        static void Main(string[] args)
        {
            Configure();
            var serviceProvider = SetupServices();
            var server = serviceProvider.GetService<ICloudMasterServer>();//new CloudMasterServer(Configuration);
            var runTask = server.Run();
            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }

        private static ServiceProvider SetupServices()
        {
            DBConfig.GetInstance(Configuration);
            var services = new ServiceCollection()
                .AddLogging()
                .AddSingleton<ICloudAuthenticationService, CloudAuthenticationService>()
                .AddSingleton<ICloudMasterServer, CloudMasterServer>()
                .AddTransient<IConfiguration>((sp) => Configuration);
            var dbOptions = Configuration.GetDbOptionsBuilder();
            services.AddManagementDbContext(Configuration);
            services.AddCache();
            services.AddTransient<UserManager<User>>();
            services.AddIdentity<User, UserRole>()
                .AddEntityFrameworkStores<ManagementDbContext>()
                .AddDefaultTokenProviders();
            services.AddTransient<IRateService, RateService>();
            services.AddTransient<UserService>();
            services.AddTransient<ApiService>();
            services.AddTransient<OrganizationService>();
            services.AddTransient<ModelService>();
            services.AddTransient<TimestampService>();
            services.AddTransient<IIntegrationService, IntegrationService>();
            services.AddTransient<ILoggingService, LoggingService>();
            services.AddSingleton<ICloudNodeService, CloudNodeService>();
            services.AddTransient<ICloudTaskService, CloudTaskService>();
            services.AddDomainAutomapper();
            services.AddRepositories();
            services.AddTransient<IDbContextFactory>((sp) => new DbContextFactory(dbOptions));
            services.AddTransient<IDbContextScopeFactory>((sp) => new DbContextScopeFactory(sp.GetService<IDbContextFactory>()));
            OrionContext = services.RegisterOrionContext(Configuration.GetSection("behaviour"), x => { }, false);

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }

        private static void Configure()
        {
            var cfgBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            Configuration = cfgBuilder.Build();
        }

        
    }
}
