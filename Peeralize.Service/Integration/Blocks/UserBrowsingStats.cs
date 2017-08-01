using MongoDB.Bson;

namespace Peeralize.Service.Integration.Blocks
{
    public class UserBrowsingStats
    {
        public long BrowsingTime { get; set; }
        public long TargetSiteTime { get; set; }
        public long TargetSiteVisits { get; set; }

        public double TargetSiteVisitAverageDuration
        {
            get
            {
                return TargetSiteVisits == 0 ? 0 : ((double)TargetSiteTime / (double)TargetSiteVisits);
            }
        }
        public long TargetSiteDomainTransitions { get; set; }
        public long TargetSiteDomainTransitionDuration { get; set; }

        public long TagetSiteDomainTransitionAverage
        {
            get
            {
                return TargetSiteDomainTransitions == 0 ? 0 : (TargetSiteDomainTransitionDuration / TargetSiteDomainTransitions);
            }
        }

        public static UserBrowsingStats FromBson(BsonValue bs)
        {
            if (bs == null) return null;
            var stats = new UserBrowsingStats();
            stats.BrowsingTime = bs["browsingTime"].AsInt64;
            stats.TargetSiteTime = bs["targetSiteTime"].AsInt64;
            stats.TargetSiteVisits = bs["targetSiteVisits"].AsInt64;
            stats.TargetSiteDomainTransitions = bs["targetSiteDomainTransitions"].AsInt64;
            stats.TargetSiteDomainTransitionDuration = bs["targetSiteDomainTransitionDuration"].AsInt64;
            return stats;
        }

        public BsonValue ToBsonDocument()
        {
            return new
            {
                browsingTime = BrowsingTime,
                targetSiteTime = TargetSiteTime,
                targetSiteVisits = TargetSiteVisits,
                targetSiteDomainTransitions = TargetSiteDomainTransitions,
                targetSiteDomainTransitionDuration = TargetSiteDomainTransitionDuration,
                targetSiteVisitAverageDuration = TargetSiteVisitAverageDuration,
                tagetSiteDomainTransitionAverage = TagetSiteDomainTransitionAverage
            }.ToBsonDocument();
        } 
    }
}