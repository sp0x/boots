using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nvoid.db.DB.Configuration;
using Netlyt.Interfaces;
using Netlyt.Service.Data;

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
