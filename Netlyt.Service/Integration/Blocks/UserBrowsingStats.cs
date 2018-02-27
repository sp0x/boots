using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using nvoid.db.Caching;

namespace Netlyt.Service.Integration.Blocks
{
    public class UserBrowsingStats
    {
        /// <summary>
        /// The seconds that were spent browsing
        /// </summary>
        public long BrowsingTime { get; set; }
        public long TargetSiteTime { get; set; }
        public long TargetSiteVisits { get; set; }
        /// <summary>
        /// The time the user spends on mobile websites
        /// </summary>
        public double TimeOnMobileSites { get; set; }
        /// <summary>
        /// The number of times that domains have transitioned.
        /// </summary>
        public long DomainChanges { get; set; }
        /// <summary>
        /// The times the user went to different domains on weekends
        /// </summary>
        public long WeekendVisits { get; set; }
        public Dictionary<string, int> GenderVisits { get; set; }
        public Dictionary<string, int> GenderPurchases { get; set; }

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
 
        public UserBrowsingStats()
        {
            GenderVisits = new Dictionary<string, int>();
            GenderPurchases = new Dictionary<string, int>();
        }

        public UserBrowsingStats AddGenderVisit(string gender)
        {
            if (GenderVisits.ContainsKey(gender)) GenderVisits[gender] = 1;
            else GenderVisits[gender]++;
            return this;
        }

        public UserBrowsingStats AddGenderPurchases(string gender)
        {
            if (GenderPurchases.ContainsKey(gender)) GenderPurchases[gender] = 1;
            else GenderPurchases[gender]++;
            return this;
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
            stats.TimeOnMobileSites = bs["timeOnMobileSites"].AsDouble;
            stats.WeekendVisits = bs["weekendVisits"].AsInt64;
            stats.DomainChanges = bs["domainChanges"].AsInt64;
            stats.GenderVisits = BsonSerializer.Deserialize<Dictionary<string, int>>(bs["genderVisits"].ToBsonDocument());
            stats.GenderPurchases = BsonSerializer.Deserialize<Dictionary<string, int>>(bs["genderPurchases"].ToBsonDocument());
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
                targetSiteDomainTransitionAverage = TagetSiteDomainTransitionAverage,
                timeOnMobileSites = TimeOnMobileSites,
                weekendVisits = WeekendVisits,
                domainChanges = DomainChanges,
                genderVisits = GenderVisits.ToBsonDocument(),
                genderPurchases = GenderPurchases.ToBsonDocument()
            }.ToBsonDocument();
        } 
    }
}