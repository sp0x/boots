using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Donut.Data;
using EntityFramework.DbContextScope;
using EntityFramework.DbContextScope.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nvoid.db.DB.Configuration;
using Netlyt.Data.ViewModels;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Cloud;
using Netlyt.Service.Cloud.Slave;
using Netlyt.Service.Data;
using Netlyt.Service.Helpers;
using Netlyt.Service.Repisitories;

namespace Netlyt.Service
{
    public static class Extensions
    {
        public static IEnumerable<ModelTarget> ToModelTargets(this IEnumerable<TargetSelectionViewModel> viewmodels, DataIntegration integration)
        {
            foreach (var vm in viewmodels)
            {
                var modelTarget = new ModelTarget(integration.GetField(vm.FieldId));
                if (vm.TimeShift != null)
                {
                    var constraint = vm.TimeShift.TimeToTargetConstraint(integration.DataTimestampColumn);
                    if (constraint != null)
                    {
                        modelTarget.Constraints.Add(constraint);
                    }
                }
                yield return modelTarget;
            }
        }

        public static TargetConstraint TimeToTargetConstraint(this TimeConstraintViewModel timeshift, string timestampColumn)
        {
            if (string.IsNullOrEmpty(timestampColumn)) return null;
            var constraint = new TargetConstraint();
            constraint.Type = TargetConstraintType.Time;
            constraint.Key = timestampColumn;
            constraint.After = new TimeConstraint();
            constraint.After.Years = timeshift.Year;
            constraint.After.Months = timeshift.Month;
            constraint.After.Days = timeshift.Day;
            constraint.After.Hours = timeshift.Hour;
            return constraint;
        }
        public static void AddDomainAutomapper(this IServiceCollection sp)
        {
            sp.AddTransient(p => new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new DomainMapProfile(p));
            }).CreateMapper());
        }

        public static void AddCloudComs(this IServiceCollection services)
        {
            services.AddTransient<INotificationService, NotificationService>();
            services.AddSingleton<ICloudNodeService, CloudNodeService>();
            services.AddTransient<ICloudTaskService, CloudTaskService>();
            services.AddSingleton<ISlaveConnector, SlaveConnector>();
            services.AddHostedService<BackgroundServiceStarter<ISlaveConnector>>();
        }
        public static void AddRepositories(this IServiceCollection services)
        {
            services.AddTransient<IIntegrationRepository, IntegrationRepository>(sp =>
            {
                return new IntegrationRepository(new AmbientDbContextLocator());
            });
            services.AddTransient<IUsersRepository, UserRepository>(sp =>
            {
                return new UserRepository(new AmbientDbContextLocator());
            });
            services.AddTransient<IApiKeyRepository, ApiKeyRepository>(sp =>
            {
                return new ApiKeyRepository(new AmbientDbContextLocator());
            });
            services.AddTransient<IModelRepository, ModelRepository>(sp =>
            {
                return new ModelRepository(new AmbientDbContextLocator());
            });
            services.AddTransient<IDonutRepository, DonutRepository>(sp =>
            {
                return new DonutRepository(new AmbientDbContextLocator());
            });
        }
        public static void AddCache(this IServiceCollection services)
        {
            services.AddSingleton<IRedisCacher>(DBConfig.GetInstance().GetCacheContext());
        }

        public static void AddManagementDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            var dbOptions = configuration.GetDbOptionsBuilder();
            var postgresConnectionString = PersistanceSettings.GetPostgresConnectionString(configuration);
            Console.WriteLine("Management DB at: " + postgresConnectionString);
            services.AddDbContext<ManagementDbContext>(options =>
                {
                    options.UseNpgsql(postgresConnectionString);
                    options.UseLazyLoadingProxies();
                }
            );
            services.AddTransient<IFactory<ManagementDbContext>, DynamicContextFactory>(s =>
                new DynamicContextFactory(() =>
                {
                    return new ManagementDbContext(dbOptions.Options);
                })
            );
        }

        public static void AddApiIdentity(this IServiceCollection services)
        {
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IUserManagementService, UserManagementService>();
        }
    }
}
