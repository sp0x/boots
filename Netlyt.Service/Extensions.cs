using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using EntityFramework.DbContextScope;
using EntityFramework.DbContextScope.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nvoid.db.DB.Configuration;
using Netlyt.Interfaces;
using Netlyt.Service.Data;
using Netlyt.Service.Repisitories;

namespace Netlyt.Service
{
    public static class Extensions
    {
        public static void AddDomainAutomapper(this IServiceCollection sp)
        {
            sp.AddTransient(p => new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new DomainMapProfile(p.GetService<ModelService>(), p.GetService<UserService>()));
            }).CreateMapper());
        }

        public static void AddRepositories(this IServiceCollection services)
        {
            services.AddTransient<IIntegrationRepository, IntegrationRepository>(sp =>
            {
                var ctxFactory = sp.GetService<IDbContextScopeFactory>();
                var ambientDbContextLocator = new AmbientDbContextLocator();
                return new IntegrationRepository(ambientDbContextLocator);
            });
            services.AddTransient<IUsersRepository, UserRepository>(sp =>
            {
                var ctxFactory = sp.GetService<IDbContextScopeFactory>();
                var ambientDbContextLocator = new AmbientDbContextLocator();
                return new UserRepository(ambientDbContextLocator);
            });
            services.AddTransient<IApiKeyRepository, ApiKeyRepository>(sp =>
            {
                var ctxFactory = sp.GetService<IDbContextScopeFactory>();
                var ambientDbContextLocator = new AmbientDbContextLocator();
                return new ApiKeyRepository(ambientDbContextLocator);
            });
        }
        public static void AddCache(this IServiceCollection services)
        {
            services.AddSingleton<IRedisCacher>(DBConfig.GetInstance().GetCacheContext());
        }

        public static void AddManagementDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            var postgresConnectionString = PersistanceSettings.GetPostgresConnectionString(configuration);
            Console.WriteLine("Management DB at: " + postgresConnectionString);
            services.AddDbContext<ManagementDbContext>(options =>
                {
                    options.UseNpgsql(postgresConnectionString);
                    options.UseLazyLoadingProxies();
                }
            );
        }
    }
}
