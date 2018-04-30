using System;
using System.Collections.Generic;
using System.Text;
using Donut.Caching;
using Microsoft.Extensions.Configuration;
using nvoid.db.DB.Configuration;
using Netlyt.Interfaces;
using StackExchange.Redis;

namespace Netlyt.Service.Data
{
    public static class Extensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static IRedisCacher GetCacheContext(this DBConfig config)
        {
            var psettings = config.PersistanceSettings;
            var conString = $"{psettings.Cache.Host}:{psettings.Cache.Port}";
            if (!string.IsNullOrEmpty(psettings.Cache.Arguments)) conString += "," + psettings.Cache.Arguments;
            var fnew = new RedisCacher(new RedisCacheOptions()
            {
                Configuration = conString
            });
            return fnew;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public static IConnectionMultiplexer GetCacheConnection(this IConfigurationSection section)
        {
            var host = section["host"];
            var port = section["port"];
            var args = section["arguments"];
            var conString = $"{host}:{port}";
            var connection = ConnectionMultiplexer.Connect(conString);
            return connection;
        }
        public static IRedisCacher GetCacheContext(this IConfigurationSection croot)
        {
            var host = croot["host"];
            var port = croot["port"];
            var args = croot["arguments"];
            var conString = $"{host}:{port}";
            //if (!string.IsNullOrEmpty(psettings.Cache.Arguments)) conString += "," + psettings.Cache.Arguments;
            var fnew = new RedisCacher(new RedisCacheOptions()
            {
                Configuration = conString
            });
            return fnew;
        }
    }
}
