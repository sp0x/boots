using System.Collections.Concurrent;
using nvoid.db.Caching;
using Netlyt.Service.Integration;
using Netlyt.Service.Models;

namespace Netlyt.Service.Donut
{
    public class NetinfoDonutContext : DonutContext
    {
        /// Donutfile meta key (page stats in this case)
        public ConcurrentDictionary<string, PageStats> PageStats { get; set; }
        public ConcurrentBag<string> Purchases { get; set; }
        public ConcurrentBag<string> PayingUsers { get; set; }
        public ConcurrentBag<string> PurchasesOnHolidays { get; set; }
        public ConcurrentBag<string> PurchasesBeforeHolidays { get; set; }
        public ConcurrentBag<string> PurchasesBeforeWeekends { get; set; }
        public ConcurrentBag<string> PurchasesInWeekends { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacher"></param>
        public NetinfoDonutContext(RedisCacher cacher, DataIntegration intd)
            : base(cacher, intd)
        { 
            PageStats = new ConcurrentDictionary<string, PageStats>();
            Purchases = new ConcurrentBag<string>();
            PayingUsers = new ConcurrentBag<string>();
            PurchasesOnHolidays = new ConcurrentBag<string>();
            PurchasesBeforeHolidays = new ConcurrentBag<string>();
            PurchasesBeforeWeekends = new ConcurrentBag<string>();
            PurchasesInWeekends = new ConcurrentBag<string>();
        }

    }
}