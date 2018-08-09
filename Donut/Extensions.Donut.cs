using System;
using System.Collections.Generic;
using System.Text;
using Donut.Orion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Netlyt.Interfaces.Data;

namespace Donut
{
    public static partial class Extensions
    {
        public static void RegisterOrionContext(this IServiceCollection services, IConfigurationSection config, Action<IOrionContext> setup)
        {
            services.AddSingleton<IOrionContext>((sp) =>
            {
                var ctx = new OrionContext(sp.GetService<IConfiguration>());
                setup(ctx);
                ctx.Configure(config);
                ctx.Run();
                return ctx;
            });
        }

        public static void AddDonutDb(this IServiceCollection services, DonutDbConfig orUseConfig)
        {
            services.AddTransient<IDatabaseConfiguration>((sp) =>
            {
                return DonutDbConfig.GetOrAdd("general", orUseConfig);
            });
        }
    }
}
