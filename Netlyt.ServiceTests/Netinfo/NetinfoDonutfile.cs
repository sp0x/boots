using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using nvoid.db.Caching;
using nvoid.db.DB;
using nvoid.extensions;
using Netlyt.Service.Donut;
using Netlyt.Service.Integration; 
using Netlyt.Service.Time;

namespace Netlyt.ServiceTests.Netinfo
{
    public class NetinfoDonutfile : Donutfile<NetinfoDonutContext>
    {
        private string TargetHost { get; set; } = "ebag.bg";
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacher"></param>
        public NetinfoDonutfile(RedisCacher cacher, IServiceProvider serviceProvider) : base(cacher, serviceProvider)
        {
            ReplayInputOnFeatures = true;
        }

        #region Overrides

        protected override void OnCreated()
        {
            base.OnCreated();
            Context.TruncateSets();
            Context.TruncateMeta(META_SPENT_TIME);
            Context.TruncateMeta(META_DOMAIN_CHANGE);
            Context.TruncateMeta(META_AGE);
            Context.TruncateMeta(META_GENDER);
            Context.TruncateMeta(META_NUMERIC_TYPE_VALUE);
        }

        protected override void OnMetaComplete()
        {

        }
        
        #endregion

        #region Gather info
        const int META_NUMERIC_TYPE_VALUE = 1;
        private const int META_GENDER = 2;
        private const int META_AGE = 3;
        private const int META_DOMAIN_CHANGE = 4;
        private const int META_SPENT_TIME = 5;

        /// <summary>
        /// Gather all the data and create any kinds of stats
        /// </summary> 
        /// <param name="intDoc"></param>
        public override void ProcessRecord(IntegratedDocument intDoc)
        {
            //TODO: Use meta categories for age, gender, agePurchased, genderPurchased groups because we just need the count (we can use the value also with meta values)
            var entry = intDoc?.Document?.Value;
            if (entry == null) return;
            var events = entry["events"] as BsonArray;
            var lastDomainVisitDuration = TimeSpan.Zero;
            var completeBrowsingDuration = TimeSpan.Zero;
            var timeSpentOnTargetDomain = TimeSpan.Zero;
            var timeSpentOnMobileSites = TimeSpan.Zero;
            var targetDomainVisits = 0;
            var targetDomainTransitionDuration = lastDomainVisitDuration;
            var targetDomainTransitionCount = 0;
            int weekendDomainVisits = 0;
            int domainChanges = 0;

            var uuid = entry["uuid"].ToString();
            var demography = Context.Demograpy.AsQueryable().FirstOrDefault(x => x["uuid"] == uuid);
            var firstEvent = events.Count > 0 ? events[0] : null;
            var lastDomain = firstEvent != null ? firstEvent["value"].ToString().ToHostname().ToLower() : null;
            DateTime? lastDomainSessionStart = firstEvent != null ? (DateTime?)DateTime.Parse(firstEvent["ondate"].ToString()) : null;

            var userTargetleadingHosts = new HashSet<string>();
            int? age = 0;
            char gender = '\0';
            if (demography != null)
            { 
                age = demography.GetInt("age");
                var genderStr = demography["gender"].ToString();
                gender = genderStr.Length>0 ? genderStr[0] : '\0';
            }
            var userStats = new UserBrowsingStats();
            var userIsPaying = false;
            //var userObj = Context.UserCookies.AddOrMerge(uuid, new NetinfoUserCookie {Uuid = uuid});
            foreach (var raw_event in events)
            {
                var @event = raw_event as BsonDocument;
                var crUrl = @event.GetString("value");
                var onDate = @event.GetDate("ondate").Value;
                var type = @event.GetInt("type");
                if (crUrl.IsNumeric())
                {
                    var metaVal = $"{type}_{crUrl}";
                    if (String.IsNullOrEmpty(uuid)) return;
                    Context.IncrementMetaCategory(META_NUMERIC_TYPE_VALUE, metaVal);
                    Context.AddEntityMetaCategory(uuid, META_NUMERIC_TYPE_VALUE, metaVal);
                }
                //Save gender
                Context.IncrementMetaCategory(META_GENDER, new string(new char[] { gender }));
                Context.AddEntityMetaCategory(uuid, META_GENDER, new string(new char[] { gender }));
                Context.IncrementMetaCategory(META_AGE, age.ToString());
                Context.AddEntityMetaCategory(uuid, META_AGE, age.ToString());

                var crDomain = crUrl.ToHostname();
                var pageSelector = crDomain;
                var pageStats = new PageStats(crUrl, 1);
                var globalPageStats = new PageStats("global", 1);
                var mobilePageStats = new PageStats("mobile", 1);
                if(!userIsPaying) userIsPaying = HandlePurchaseUrl(crUrl, crDomain, onDate, uuid);
                //Handle domain change
                if (crDomain != lastDomain)
                {
                    // Domain has changed, add the time from the last domain
                    var visitDuration = onDate - lastDomainSessionStart.Value;
                    if (visitDuration.TotalSeconds == 0) visitDuration = visitDuration.Add(TimeSpan.FromSeconds(1));
                    pageStats.TotalTransitionDuration += visitDuration;
                    pageStats.Transitions += 1;
                    Context.IncrementMetaCategory(META_DOMAIN_CHANGE, "global");
                    if(onDate.DayOfWeek>DayOfWeek.Friday) Context.IncrementMetaCategory(META_DOMAIN_CHANGE, "weekend");
                    globalPageStats.VisitDuration += visitDuration;
                    globalPageStats.Transitions += 1;
                    completeBrowsingDuration += visitDuration;

                    //Context.DomainTransitions.Count += 1;
                    if (crDomain.IsMobileDomain())
                    {
                        mobilePageStats.VisitDuration += visitDuration;
                        timeSpentOnMobileSites += visitDuration;
                    }
                    //We were on the target domain
                    if (lastDomain == TargetHost)
                    {
                        timeSpentOnTargetDomain += visitDuration;
                        targetDomainVisits++;
                    }
                    else
                    {
                        //We went to the target host
                        if (crDomain == TargetHost)
                        {
                            targetDomainTransitionDuration += lastDomainVisitDuration;
                            targetDomainTransitionCount++;
                            var stats = Context.PageStats.GetOrAddHash(lastDomain);
                            stats.AddFollowingSite(TargetHost);
                            var followingReference = stats
                                .FollowingReferences[crDomain];
                            if (!userTargetleadingHosts.Contains(lastDomain))
                            {
                                //followingReference.UsersVisitedTotal++;
                                //Increase the paying members, which were led
                                if (userIsPaying)
                                {
                                    followingReference.PurchasedUsers++;
                                }
                                userTargetleadingHosts.Add(lastDomain);
                            }
                            followingReference.TotalTransitionDuration += lastDomainVisitDuration;
                            followingReference.AddVisit(uuid, lastDomainVisitDuration);//TransitionCount++;
                        }
                    } 
                    //Set our last domain visit to this current visit
                    lastDomainSessionStart = onDate;
                    lastDomainVisitDuration = visitDuration;
                }
                lastDomain = crDomain;
                Context.PageStats.AddOrMerge(pageSelector, pageStats);
                Context.PageStats.AddOrMerge("global", globalPageStats);
                Context.PageStats.AddOrMerge("mobile", mobilePageStats);
            }
            if (userIsPaying)
            { 
                var visitingTimeFrac = (double)(completeBrowsingDuration.TotalSeconds == 0
                    ? 0
                    : (timeSpentOnTargetDomain.TotalSeconds / completeBrowsingDuration.TotalSeconds));
                Context.AddEntityMetaCategory("spent_time", META_SPENT_TIME, visitingTimeFrac,true);  
            }
            userStats.BrowsingTime = (int)completeBrowsingDuration.TotalSeconds;
            userStats.DomainChanges = (int)domainChanges;
            userStats.TargetSiteTime = (int)timeSpentOnTargetDomain.TotalSeconds;
            userStats.TargetSiteVisits = targetDomainVisits;
            userStats.TargetSiteDomainTransitionDuration = (int)targetDomainTransitionDuration.TotalSeconds;
            userStats.TargetSiteDomainTransitions = targetDomainTransitionCount;
            userStats.TimeOnMobileSites = timeSpentOnMobileSites.TotalSeconds;
            userStats.WeekendVisits = weekendDomainVisits;
            Context.UserBrowsingStats.AddOrMerge(uuid, userStats);

            Context.CacheAndClear();
        }


        public double GetLongestWebSessionDuration()
        {
            var value = Context.MetaEntityMax("spent_time", META_SPENT_TIME, 1);
            return value;
        }

        private bool HandlePurchaseUrl(string value, string pageHost, DateTime onDate, string uuid)
        {
            if (value.Contains("payments/finish") && pageHost.Contains(TargetHost))
            {
                if (DateHelper.IsHoliday(onDate))
                {
                    Context.PurchasesOnHolidays.Add(uuid);
                }
                else if (DateHelper.IsHoliday(onDate.AddDays(1))) Context.PurchasesBeforeHolidays.Add(uuid);
                else if (onDate.DayOfWeek == DayOfWeek.Friday) Context.PurchasesBeforeWeekends.Add(uuid);
                else if (onDate.DayOfWeek > DayOfWeek.Friday) Context.PurchasesInWeekends.Add(uuid);
                Context.Purchases.Add(uuid);
                Context.PayingUsers.Add(uuid); //["is_paying"] = 1;
                return true;
            }
            return false;
        }

        #endregion

        #region Domain tracking

        public IEnumerable<DomainUserSession> GetWebSessions(IntegratedDocument doc, string targetDomain)
        {
            return GetWebSessions(doc)
                .Where(x => x.Domain.ToHostname()
                .ToLower().Contains(targetDomain));
        }

        /// <summary>
        /// Gets the web browsing sessions that the user made
        /// </summary>
        /// <param name="userDoc"></param>
        /// <returns></returns>
        public static IEnumerable<DomainUserSession> GetWebSessions(IntegratedDocument document)
        {
            var visits = document.GetDocument()["events"] as BsonArray;
            var firstVisit = visits[0];
            var lastDomain = firstVisit != null ? firstVisit["value"].ToString().ToHostname().ToLower() : null;
            DateTime? lastDomainSessionStart = null;
            if (firstVisit != null) lastDomainSessionStart = DateTime.Parse(firstVisit["ondate"].ToString());
            var lastDomainVisitDuration = TimeSpan.Zero;

            for (int i = 1; i < visits.Count; i++)
            {
                var visit = visits[i];
                var currentUrl = visit["value"].ToString();
                var crDomain = currentUrl.ToHostname().ToLower();
                var visited = DateTime.Parse(visit["ondate"].ToString());
                if (crDomain == lastDomain)
                {
                    //We're still in the same domain, nothing has changed
                }
                else
                {
                    //Domain has changed, add the time from the last domain
                    var visitDuration = visited - lastDomainSessionStart.Value;
                    //Add at least 2 seconds if duration is 0
                    if (visitDuration.TotalSeconds == 0) visitDuration = visitDuration.Add(TimeSpan.FromSeconds(1));

                    lastDomainSessionStart = visited;
                    lastDomainVisitDuration = visitDuration;

                    //A session has ended, yield it
                    var session = new DomainUserSession(lastDomain, visited, visitDuration);

                    yield return session;
                }
                lastDomain = crDomain;

            }
            if (visits.Count > 1)
            {
                var visit = visits[visits.Count - 1];
                var currentUrl = visit["value"].ToString();
                var crDomain = currentUrl.ToHostname().ToLower();
                var visited = lastDomainSessionStart.Value;
                var visitEnd = DateTime.Parse(visit["ondate"].ToString());
                var duration = visitEnd - visited;
                if (duration.TotalSeconds <= 0) duration = TimeSpan.FromSeconds(1);

                var session = new DomainUserSession(crDomain, visited, duration);
                yield return session;
            }
            else
            {
                //Yield the last domain
                if (firstVisit != null)
                {
                    var currentUrl = firstVisit["value"].ToString();
                    var crDomain = currentUrl.ToHostname().ToLower();
                    var visited = DateTime.Parse(firstVisit["ondate"].ToString());
                    var session = new DomainUserSession(crDomain, visited, TimeSpan.FromSeconds(1));
                    yield return session;
                }
            }
        }

        #endregion

        #region Helper methods to generate features
        public int GetUsersWithSameAgeCount(int userAge)
        {
            throw new NotImplementedException();
        }

        public int GetBuyersWithSameAgeCount(int userAge)
        {
            throw new NotImplementedException();
        }

        public int GetUsersWithSameAgeAndGenderCount(int userAge, string userGender)
        {
            throw new NotImplementedException();
        }

        public int GetBuyersWithSameGenderCount(string userGender)
        {
            throw new NotImplementedException();
        }

        public int GetUsersWithSameGenderCount(string userGender)
        {
            throw new NotImplementedException();
        }

        public int GetDomainVisitorsCount(string targetDomain)
        {
            throw new NotImplementedException();
        }

        public int GetEntityCount()
        {
            throw new NotImplementedException();
        }

        public double GetAveragePageRating(IEnumerable<IGrouping<string, BsonValue>> domainVisits, string targetDomain, bool b)
        {
            throw new NotImplementedException();
        }

        public int TargetDomainVisitorsCount(int usersWithSameGender)
        {
            throw new NotImplementedException();
        }

        public int GetAverageDomainVisits()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<PageStats> GetHighrankingPages(string targetDomain, int i)
        {
            throw new NotImplementedException();
        }

        public double GetLongestVisitPurchaseDuration(string targetDomain, string paymentsFinishUrl)
        {
            throw new NotImplementedException();
        }

        public double GetBuyVisitValues()
        {
            throw new NotImplementedException();
        }

        public void AddBuyVisitValue(double probBuyVisit)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}