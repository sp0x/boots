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
        public static IOrionContext RegisterOrionContext(this IServiceCollection services, IConfigurationSection config, Action<IOrionContext> setup, bool run = true)
        {
            var ctx = new OrionContext(config);
            setup(ctx);
            ctx.Configure(config);
            if(run) ctx.Run();
            services.AddSingleton<IOrionContext>(ctx);
            return ctx;
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
