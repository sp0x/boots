using System;
using Microsoft.Extensions.DependencyInjection;

namespace Netlyt.Service.Donut
{
    public static class Extensions
    {
        public static DonutOrionHandler GetOrionHandler(this IServiceProvider services)
        {
            var svc = services.GetService<DonutOrionHandler>();
            if (svc == null) throw new Exception("Orion handler service not registered!");
            return svc;
        }
    }
}