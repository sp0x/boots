using System;
using System.IO;
using System.Text;
using Donut.Orion;
using EntityFramework.DbContextScope;
using EntityFramework.DbContextScope.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nvoid.db.DB.Configuration;
using Netlyt.Interfaces;
using Netlyt.Service;
using Netlyt.Service.Cloud;
using Netlyt.Service.Cloud.Interfaces;
using Netlyt.Service.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Netlyt.Master
{
    class Program
    {
        public static IConfigurationRoot Configuration { get; set; }

        static void Main(string[] args)
        {
            var serviceProvider = SetupServices();
            Configure();
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
            var postgresConnectionString = PersistanceSettings.GetPostgresConnectionString(Configuration);
            Console.WriteLine("Management DB at: " + postgresConnectionString);
            services.AddDbContext<ManagementDbContext>(options =>
                {
                    options.UseNpgsql(postgresConnectionString);
                    options.UseLazyLoadingProxies();
                }
            );
            services.AddCache();
            services.AddTransient<IRateService, RateService>();
            services.AddTransient<IDbContextFactory>((sp) => new DbContextFactory(dbOptions));
            services.AddTransient<IDbContextScopeFactory>((sp) => new DbContextScopeFactory(sp.GetService<IDbContextFactory>()));

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
