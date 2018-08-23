using System;
using System.Collections.Generic;
using System.Text;
using Donut;
using Donut.Orion;
using EntityFramework.DbContextScope;
using EntityFramework.DbContextScope.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nvoid.db.DB.Configuration;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Cloud;
using Netlyt.Service.Data;
using Netlyt.Service.Donut;

namespace Netlyt.Service
{
    public class NetlytService
    {
        public static ServiceProvider SetupBackgroundServices(DbContextOptionsBuilder<ManagementDbContext> dbContextBuilder,
            IConfiguration configuration, IOrionContext orionContext)
        {
            var postgresConnectionString = PersistanceSettings.GetPostgresConnectionString(configuration);
            var services = new ServiceCollection();
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
            services.AddDomainAutomapper();
            services.AddDonutDb(DBConfig.GetInstance().GetGeneralDatabase().ToDonutDbConfig());
            services.AddSingleton(configuration);
            services.AddDistributedMemoryCache(); // Adds a default in-memory implementation of IDistributedCache 
            services.AddCache();
            // Add application services.
            //services.AddSingleton<RoutingConfiguration>(new RoutingConfiguration(configuration));
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IOrionContext>(orionContext);
            services.AddSingleton<SocialNetworkApiManager>(new SocialNetworkApiManager());
            services.AddTransient<UserManager<User>>();
            services.AddTransient<SignInManager<User>>();
            services.AddTransient<CompilerService>();
            services.AddTransient<IDonutService, DonutService>();
            services.AddTransient<IEmailSender, AuthMessageSender>((sp) => new AuthMessageSender(configuration));

            services.AddTransient<IFactory<ManagementDbContext>, DynamicContextFactory>(s =>
                new DynamicContextFactory(() => new ManagementDbContext(dbContextBuilder.Options))
            );
            services.AddTransient<IDbContextFactory>((sp) => new DbContextFactory(dbContextBuilder));
            services.AddTransient<IDbContextScopeFactory>((sp) => new DbContextScopeFactory(sp.GetService<IDbContextFactory>()));
            services.AddTransient<ILogger>(x => x.GetService<ILoggerFactory>().CreateLogger("Netlyt.Service.Logs"));
            services.AddTransient<IRateService, RateService>();
            services.AddTransient<UserService>();
            services.AddTransient<ApiService>();
            services.AddTransient<TimestampService>();
            services.AddTransient<ModelService>();
            services.AddTransient<OrganizationService>();
            services.AddTransient<IIntegrationService, IntegrationService>();
            services.AddTransient<ICloudTaskService, CloudTaskService>();
            services.AddTransient<PermissionService>();
            //services.AddDomainAutomapper();
            services.AddTransient<TrainingHandler>();
            services.AddSingleton<DonutOrionHandler>();
            services.AddRepositories();

            var backgroundServiceProvider = services.BuildServiceProvider();
            var orionHandler = backgroundServiceProvider.GetOrionHandler();
            return backgroundServiceProvider;
        }
    }
}
