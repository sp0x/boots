using System;
using nvoid.db.Caching;

namespace Netlyt.Service.Models.CacheMaps
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