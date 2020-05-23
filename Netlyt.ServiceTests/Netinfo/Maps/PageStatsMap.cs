using System;
using Donut.Caching;
using nvoid.db.Caching;
using StackExchange.Redis;

namespace Netlyt.ServiceTests.Netinfo.Maps
{
    public class PageStatsMap : CacheMap<PageStats>
    {
        public override void Map()
        {
            AddMember(x => x.PurchasedUsers)
                .Merge((a, b) => a.PurchasedUsers += b.PurchasedUsers)
                .AddMember(x => x.Transitions)
                .Merge((a, b) => a.Transitions += b.Transitions)
                .AddMember(x => x.PageVisitsTotal)
                .Merge((a, b) => a.PageVisitsTotal += b.PageVisitsTotal);
            AddMember(x => x.TotalTransitionDuration.Ticks, "TotalTransitionDuration")
                .DeserializeAs((RedisValue hash) => new TimeSpan((long)hash))
                .Merge((a, b) => a.TotalTransitionDuration += b.TotalTransitionDuration);
        }
    }
}