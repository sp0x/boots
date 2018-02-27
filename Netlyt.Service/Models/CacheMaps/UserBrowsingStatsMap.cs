using nvoid.db.Caching;
using Netlyt.Service.Integration.Blocks;

namespace Netlyt.Service.Models.CacheMaps
{
    public class UserBrowsingStatsMap : CacheMap<UserBrowsingStats>
    {
        public override void Map()
        {
            AddMember(x => x.BrowsingTime);
            AddMember(x => x.TargetSiteTime);
            AddMember(x => x.TargetSiteVisits);
            AddMember(x => x.TimeOnMobileSites);
            AddMember(x => x.DomainChanges);
            AddMember(x => x.WeekendVisits);
            AddMember(x => x.TargetSiteDomainTransitions);
            AddMember(x => x.TargetSiteDomainTransitionDuration);
        }
    }
}