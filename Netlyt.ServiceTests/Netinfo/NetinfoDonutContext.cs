using System;
using Donut;
using Donut.Caching;
using MongoDB.Bson;
using nvoid.db;
using nvoid.db.Caching;
using Netlyt.Interfaces;
using Netlyt.Service.Donut;
using Netlyt.Service.Integration;

using Netlyt.ServiceTests.Netinfo.Maps;
using DataIntegration = Donut.Data.DataIntegration;

namespace Netlyt.ServiceTests.Netinfo
{ 
    public class NetinfoDonutContext : DonutContext
    {
        /// Donutfile meta key (page stats in this case) 
        public CacheSet<PageStats> PageStats { get; set; } 
        public CacheSet<UserBrowsingStats> UserBrowsingStats { get; set; }
        public CacheSet<string> Purchases { get; set; }
        public CacheSet<string> PayingUsers { get; set; }
        public CacheSet<string> PurchasesOnHolidays { get; set; }
        public CacheSet<string> PurchasesBeforeHolidays { get; set; }
        public CacheSet<string> PurchasesBeforeWeekends { get; set; }
        public CacheSet<string> PurchasesInWeekends { get; set; }

        [SourceFromIntegration("Demography")]
        public DataSet<BsonDocument> Demograpy { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacher"></param>
        public NetinfoDonutContext(RedisCacher cacher, DataIntegration intd, IServiceProvider serviceProvider)
            : base(cacher, intd, serviceProvider)
        { 
        }

        protected override void ConfigureCacheMap()
        { 
            RedisCacher.RegisterCacheMap<PageStatsMap, PageStats>();
            RedisCacher.RegisterCacheMap<DomainUserSessionMap, DomainUserSession>();
            RedisCacher.RegisterCacheMap<UserBrowsingStatsMap, UserBrowsingStats>();
            RedisCacher.RegisterCacheMap<NetinfoUserCookeMap, NetinfoUserCookie>();
        }

    }
}