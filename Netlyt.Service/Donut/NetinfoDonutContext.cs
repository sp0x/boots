﻿using System;
using System.Collections.Concurrent;
using System.Dynamic;
using MongoDB.Bson;
using MongoDB.Driver;
using nvoid.db;
using nvoid.db.Caching;
using nvoid.Integration;
using Netlyt.Service.Integration;
using Netlyt.Service.Integration.Blocks;
using Netlyt.Service.Models;
using Netlyt.Service.Models.CacheMaps;
using Netlyt.Service.Models.Netinfo;

namespace Netlyt.Service.Donut
{ 
    public class NetinfoDonutContext : DonutContext
    {
        /// Donutfile meta key (page stats in this case)
        [CacheBacking(CacheType.Hash)]
        public CacheSet<PageStats> PageStats { get; set; }
        [CacheBacking(CacheType.Hash)]
        public CacheSet<UserBrowsingStats> UserBrowsingStats { get; set; }
//        [CacheBacking(CacheType.Hash)]
//        public CacheSet<NetinfoUserCookie> UserCookies { get; set; }
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