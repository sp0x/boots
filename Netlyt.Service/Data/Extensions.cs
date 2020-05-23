using System;
using System.Collections.Generic;
using System.Text;
using Donut.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using StackExchange.Redis;

namespace Netlyt.Service.Data
{
    public static class Extensions
    {
        public static byte[][] Split(this byte[] source, byte[] separator)
        {
            var Parts = new List<byte[]>();
            var Index = 0;
            byte[] Part;
            for (var I = 0; I < source.Length; ++I)
            {
                if (Equals(source, separator, I))
                {
                    Part = new byte[I - Index];
                    Array.Copy(source, Index, Part, 0, Part.Length);
                    Parts.Add(Part);
                    Index = I + separator.Length;
                    I += separator.Length - 1;
                }
            }
            Part = new byte[source.Length - Index];
            Array.Copy(source, Index, Part, 0, Part.Length);
            Parts.Add(Part);
            return Parts.ToArray();
        }
        private static bool Equals(byte[] source, byte[] separator, int index)
        {
            for (int i = 0; i < separator.Length; ++i)
                if (index + i >= source.Length || source[index + i] != separator[i])
                    return false;
            return true;
        }
        public static DbContextOptionsBuilder<ManagementDbContext> GetDbOptionsBuilder(this IConfiguration configuration)
        {
            var postgresConnectionString = PersistanceSettings.GetPostgresConnectionString(configuration);
            Console.WriteLine("Management DB at: " + postgresConnectionString);
            var dbOptions = new DbContextOptionsBuilder<ManagementDbContext>()
                .UseNpgsql(postgresConnectionString)
                .UseLazyLoadingProxies();
            return dbOptions;
        }
        public static DonutDbConfig ToDonutDbConfig(this DatabaseConfiguration dbc)
        {
            var ddb = new DonutDbConfig(dbc.Name, dbc.Role, dbc.Value);
            switch (dbc.Type)
            {
                case nvoid.db.DB.DatabaseType.MongoDb:
                    ddb.Type = Interfaces.Data.DatabaseType.MongoDb;
                    break;
                case nvoid.db.DB.DatabaseType.MySql:
                    ddb.Type = Interfaces.Data.DatabaseType.MySql;
                    break;
            }
            return ddb;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static IRedisCacher GetCacheContext(this DBConfig config)
        {
            var psettings = config.PersistanceSettings;
            var cacheConfig = psettings.GetCacheConfig();
            var conString = $"{cacheConfig.Host}:{cacheConfig.Port}";
            if (!string.IsNullOrEmpty(cacheConfig.Arguments)) conString += "," + cacheConfig.Arguments;
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
