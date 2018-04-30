using Donut.Caching;
using nvoid.db.Caching;
using Netlyt.Service.Integration.Blocks;

namespace Netlyt.ServiceTests.Netinfo.Maps
{
    public class UserBrowsingStatsMap : CacheMap<UserBrowsingStats>
    {
        public override void Map()
        {
            AddMember(x => x.BrowsingTime)
                .Merge((a, b) => a.BrowsingTime += b.BrowsingTime);
            AddMember(x => x.TargetSiteTime)
                .Merge((a, b) => a.TargetSiteTime += b.TargetSiteTime);
            AddMember(x => x.TargetSiteVisits)
                .Merge((a, b) => a.TargetSiteVisits += b.TargetSiteVisits);
            AddMember(x => x.TimeOnMobileSites)
                .Merge((a, b) => a.TimeOnMobileSites += b.TimeOnMobileSites);
            AddMember(x => x.DomainChanges)
                .Merge((a, b) => a.DomainChanges += b.DomainChanges);
            AddMember(x => x.WeekendVisits)
                .Merge((a, b) => a.WeekendVisits += b.WeekendVisits);
            AddMember(x => x.TargetSiteDomainTransitions)
                .Merge((a, b) => a.TargetSiteDomainTransitions += b.TargetSiteDomainTransitions);
            AddMember(x => x.TargetSiteDomainTransitionDuration)
                .Merge((a, b) => a.TargetSiteDomainTransitionDuration += b.TargetSiteDomainTransitionDuration);
        }
    }
}