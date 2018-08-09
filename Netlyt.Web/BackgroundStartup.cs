using System;
using AutoMapper;
using Donut;
using Donut.Orion;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nvoid.db.DB.Configuration;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.Service.Donut;
using Netlyt.Web.Extensions;
using Netlyt.Web.Services;

namespace Netlyt.Web
{
    public partial class Startup
    {
        public ServiceProvider BackgroundServiceProvider { get; set; }
        public DonutOrionHandler OrionHandler { get; set; }
        public void ConfigureBackgroundServices(IServiceProvider mainServices)
        {
            var services = new ServiceCollection();
            var postgresConnectionString = PersistanceSettings.GetPostgresConnectionString(Configuration);
            services.AddDbContext<ManagementDbContext>(options =>
                {
                    options.UseLazyLoadingProxies();
                    options.UseNpgsql(postgresConnectionString);
                }
            );
            services.AddIdentity<User, UserRole>()
                .AddEntityFrameworkStores<ManagementDbContext>()
                .AddDefaultTokenProviders();
            services.AddMemoryCache();
            services.AddSession();
            services.AddDonutDb(DBConfig.GetInstance().GetGeneralDatabase().ToDonutDbConfig());
            services.AddSingleton(Configuration);
            services.AddDistributedMemoryCache(); // Adds a default in-memory implementation of IDistributedCache 
            services.AddSingleton<IRedisCacher>(DBConfig.GetInstance().GetCacheContext());
            // Add application services.
            services.AddSingleton<RoutingConfiguration>(new RoutingConfiguration(Configuration));
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IOrionContext>(OrionContext);
            services.AddSingleton<SocialNetworkApiManager>(new SocialNetworkApiManager());
            services.AddTransient<UserManager<User>>();
            services.AddTransient<SignInManager<User>>();
            services.AddTransient<CompilerService>();
            services.AddTransient<IDonutService, DonutService>();
            services.AddTransient<IEmailSender, AuthMessageSender>((sp) =>
                {
                    return new AuthMessageSender(Configuration);
                });
            services.AddTransient<IFactory<ManagementDbContext>, DynamicContextFactory>(s =>
                new DynamicContextFactory(() =>
                {
                    var opsBuilder = new DbContextOptionsBuilder<ManagementDbContext>()
                        .UseNpgsql(postgresConnectionString)
                        .UseLazyLoadingProxies();
                    return new ManagementDbContext(opsBuilder.Options);
                })
            );
            services.AddTransient<ILogger>(x => x.GetService<ILoggerFactory>().CreateLogger("Netlyt.Web.Logs"));
            services.AddTransient<UserService>();
            services.AddTransient<ApiService>();
            services.AddTransient<TimestampService>();
            services.AddTransient<ModelService>();
            services.AddTransient<OrganizationService>();
            services.AddTransient<IIntegrationService, IntegrationService>();
            services.AddDomainAutomapper();

            services.AddSingleton<DonutOrionHandler>();

            BackgroundServiceProvider = services.BuildServiceProvider();
            OrionHandler = BackgroundServiceProvider.GetOrionHandler();
            OrionHandler = OrionHandler;
        }

    }
}
