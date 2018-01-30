using System.Collections.Concurrent;
using nvoid.db.Caching;
using Netlyt.Service.Integration;
using Netlyt.Service.Models;

namespace Netlyt.Service.Donut
{
    public class NetinfoDonutContext : DonutContext
    {
        /// Donutfile meta key (page stats in this case)
        public CacheHash<PageStats> PageStats { get; set; }
        public CacheSet<string> Purchases { get; set; }
        public CacheSet<string> PayingUsers { get; set; }
        public CacheSet<string> PurchasesOnHolidays { get; set; }
        public CacheSet<string> PurchasesBeforeHolidays { get; set; }
        public CacheSet<string> PurchasesBeforeWeekends { get; set; }
        public CacheSet<string> PurchasesInWeekends { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacher"></param>
        public NetinfoDonutContext(RedisCacher cacher, DataIntegration intd)
            : base(cacher, intd)
        { 
        }

    }
}