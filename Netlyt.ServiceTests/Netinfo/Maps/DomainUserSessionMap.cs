using System;
using Donut.Caching;
using nvoid.db.Caching;
using Netlyt.Service.Models;

namespace Netlyt.ServiceTests.Netinfo.Maps
{
    public class DomainUserSessionMap : CacheMap<DomainUserSession>
    {
        public override void Map()
        {
            AddMember(x => x.Domain)
                .AddMember(x => x.Duration.Ticks, "Duration")
                .DeserializeAs(hash => new TimeSpan((long)hash));
            AddMember(x => x.Visited.Ticks, "Visited")
                 .DeserializeAs(hash => new DateTime((long)hash));
        }
    }
}