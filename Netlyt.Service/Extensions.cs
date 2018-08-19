using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using nvoid.db.DB.Configuration;
using Netlyt.Interfaces;
using Netlyt.Service.Data;

namespace Netlyt.Service
{
    public static class Extensions
    {
        public static void AddCache(this IServiceCollection services)
        {
            services.AddSingleton<IRedisCacher>(DBConfig.GetInstance().GetCacheContext());
        }
    }
}
