using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MailKit;
using MongoDB.Bson;
using nvoid.extensions;
using Netlyt.Service.Models;

namespace Netlyt.Service.Integration.Blocks
{
    public class CrossSiteAnalyticsHelper
    {
        public IDictionary<object, IntegratedDocument> EntityDictionary { get; private set; }
        public CrossPageStats DomainVisitStats { get; private set; }
        //public int DomainVisitsTotal { get; private set; }
        //public Dictionary<string, int> DomainVisits { get; private set; }
        public double BuyVisitValues { get; private set; }
        public CrossPageStats Stats { get; set; }
        public Dictionary<string, List<IntegratedDocument>> AgeGroups { get; set; }
        public Dictionary<string, List<IntegratedDocument>> AgeGroupsPaying { get; set; }
        public Dictionary<string, List<IntegratedDocument>> GenderGroups { get; set; }
        public Dictionary<string, List<IntegratedDocument>> GenderGroupsPaying { get; set; }
        public Dictionary<string, List<IntegratedDocument>> AgeAndGenderGroups { get; set; }
        public Dictionary<string, List<IntegratedDocument>> DistinctUsers { get; set; }

        public BsonArray Purchases { get; private set; }
        public BsonArray PurchasesOnHolidays { get; private set; }
        public BsonArray PurchasesBeforeHolidays { get; private set; }
        public BsonArray PurchasesInWeekends { get; private set; }
        public BsonArray PurchasesBeforeWeekends { get; private set; }

        private ConcurrentDictionary<byte, Dictionary<string, Score>> _featureKeyDictionary;
        private ConcurrentDictionary<string, Dictionary<byte, HashSet<string>>> _usersWithSpecialEvents;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly object _sessionsLock;
        private readonly object _demographyLock;
        private bool _grouped;

        public CrossSiteAnalyticsHelper()
        {
            _sessionsLock = new object();
            _demographyLock = new object();
            _featureKeyDictionary = new ConcurrentDictionary<byte, Dictionary<string, Score>>();
            _usersWithSpecialEvents = new ConcurrentDictionary<string, Dictionary<byte, HashSet<string>>>();
            Purchases = new BsonArray();
            PurchasesOnHolidays = new BsonArray();
            PurchasesBeforeHolidays = new BsonArray();
            PurchasesInWeekends = new BsonArray();
            PurchasesBeforeWeekends = new BsonArray();
            DomainVisitStats = new CrossPageStats();
            HighrankPageCache = new Dictionary<string, IEnumerable<PageStats>>();
            Stats = new CrossPageStats();
            _highrankingPagesLock = new object();
        }

        public CrossSiteAnalyticsHelper(IDictionary<object, IntegratedDocument> sessions)
            :this()
        {
            EntityDictionary = sessions;
            //if (domainVisitStats != null ) this.DomainVisitStats = domainVisitStats;
            //DomainVisits = new Dictionary<string, int>();
        }

        /// <summary>
        /// Add a record that the domain vasvisited
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="visitDuration"></param>
        private void AddDomainVisit(string userKey, string domain, TimeSpan visitDuration)
        {
            DomainVisitStats.AddDomainVisit(userKey, domain, visitDuration);
            //            if (!DomainVisits.ContainsKey(domain))
            //            {
            //                DomainVisits[domain] = 1;
            //            }
            //            else DomainVisits[domain]++;
        }

        public static IEnumerable<DomainUserSession> GetWebSessions(IntegratedDocument userDoc, string targetDomain)
        {
            return GetWebSessions(userDoc).Where(x => x.Domain.ToHostname().ToLower().Contains(targetDomain));
        }

        /// <summary>
        /// Gets the web browsing sessions that the user made
        /// </summary>
        /// <param name="userDoc"></param>
        /// <returns></returns>
        public static IEnumerable<DomainUserSession> GetWebSessions(IntegratedDocument userDoc)
        {
            BsonDocument userDocDocument = userDoc.GetDocument();
            BsonArray visits = null;
            try
            {
                visits = userDocDocument.Contains("events") ? (BsonArray) userDocDocument.GetElement("events").Value : new BsonArray();
            }
            catch (Exception ex)
            {
                try
                {
                    userDocDocument = userDocDocument.Clone() as BsonDocument;
                    visits = userDocDocument.Contains("events") ? (BsonArray)userDocDocument.GetElement("events").Value : new BsonArray();
                }
                catch (Exception e2)
                {
                    Debug.WriteLine(e2.Message);
                }
            }
             
            var firstVisit = visits.Count > 0 ? (BsonDocument)visits[0] : null;
            var lastDomain = firstVisit!=null ? firstVisit["value"].ToString().ToHostname().ToLower() : null;

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

        private double? _longestVisitPurchaseDuration;
        /// <summary>
        /// Gets the max user time(secs) spent on the target domain, out of all the user's browsing times.
        /// Using: max(time_on_target_domain / time_in_all_domains) in seconds.
        /// Using this method records user browsing information in this format:
        ///  browsing_statistics = { browsingTime : seconds , targetSiteTime: seconds , targetSiteTransitionAverage : seconds}
        ///  isPaying = bool
        /// </summary>
        /// <param name="targetDomain"></param>
        /// <returns></returns>
        public double GetLongestVisitPurchaseDuration(string targetDomain, string specialUrl, bool useCache = true)
        {
            lock (_sessionsLock)
            {
                if (_longestVisitPurchaseDuration != null && useCache) return _longestVisitPurchaseDuration.Value;
                var spentTimes = new List<double>();
                //DomainVisitsTotal = 0;
                foreach (var userPair in this.EntityDictionary)
                {
                    var valueDocument = userPair.Value.GetDocument();
                    string userId = valueDocument["uuid"].ToString();//Key.ToString();
                    var visits = (BsonArray)valueDocument["events"];
                    var userIsPaying = userPair.Value.Has("is_paying") && userPair.Value.GetInt("is_paying") ==1;
                    if (userIsPaying)
                    {
                        userIsPaying = userIsPaying;
                    }
                    //var userInternetBrowsingTime = TimeSpan.Zero;

                    var firstVisit = visits.Count>0 ? visits[0] : null;
                    var lastDomain = firstVisit == null ? null : firstVisit["value"].ToString().ToHostname().ToLower();
                    var firstDomain = lastDomain==null? null : new string(lastDomain.ToCharArray());
                    var userTargetleadingHosts = new HashSet<string>();

                    DateTime? lastDomainSessionStart = null;
                    if (firstVisit != null) lastDomainSessionStart = DateTime.Parse(firstVisit["ondate"].ToString());

                    var lastDomainVisitDuration = TimeSpan.Zero;
                    var completeBrowsingDuration = TimeSpan.Zero;
                    var timeSpentOnTargetDomain = TimeSpan.Zero;
                    var timeSpentOnMobileSites = TimeSpan.Zero;
                    var targetDomainVisits = 0;
                    var targetDomainTransitionDuration = lastDomainVisitDuration;
                    var targetDomainTransitionCount = 0;

                    int weekendDomainVisits = 0;
                    int domainChanges = 0;
                    for (int i = 1; i < visits.Count; i++)
                    {
                        var visit = visits[i];
                        var currentUrl = visit["value"].ToString();
                        var crDomain = currentUrl.ToHostname().ToLower();
                        var visited = DateTime.Parse(visit["ondate"].ToString());

                        if (crDomain.Contains(targetDomain) && currentUrl.Contains(specialUrl))
                        {
                            userIsPaying = true;
                        }
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
                            AddDomainVisit(userId, lastDomain, visitDuration);

                            if (visited.DayOfWeek > DayOfWeek.Friday)
                            {
                                weekendDomainVisits++;
                            }

                            domainChanges++;

                            completeBrowsingDuration += visitDuration;
                            if (crDomain.IsMobileDomain())
                            {
                                timeSpentOnMobileSites += visitDuration;
                            }


                            //If we were on our target domain, and the current one is not any more
                            if (lastDomain == targetDomain)
                            {
                                timeSpentOnTargetDomain += visitDuration;
                                targetDomainVisits++;
                                if (lastDomainVisitDuration > TimeSpan.Zero)
                                {
                                    //targetDomainTransitionCount++;
                                }
                            }
                            else
                            {
                                //If we transitioned from another domain, to our domain
                                if (crDomain == targetDomain)
                                {
                                    targetDomainTransitionDuration += lastDomainVisitDuration;
                                    targetDomainTransitionCount++;

                                    DomainVisitStats.PageStats[lastDomain].AddFollowingSite(targetDomain);
                                    var followingReference = DomainVisitStats.PageStats[lastDomain]
                                        .FollowingReferences[crDomain];
                                    //If the current user has not marked that he has visited the site, leading to the targetDomain
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
                                    followingReference.AddVisit(userId, lastDomainVisitDuration);//TransitionCount++;
                                                                                                 //Add the tranition domain
                                }
                            }

                            //Set our last domain visit to this current visit
                            lastDomainSessionStart = visited;
                            lastDomainVisitDuration = visitDuration;
                        }
                        lastDomain = crDomain;
                    }
                    if (visits.Count == 1)
                    {
                        AddDomainVisit(userId, lastDomain, lastDomainVisitDuration);
                    }

                    if (userIsPaying)
                    {
                        var visitingTimeFrac = (double)(completeBrowsingDuration.TotalSeconds == 0
                            ? 0
                            : (timeSpentOnTargetDomain.TotalSeconds / completeBrowsingDuration.TotalSeconds));
                        spentTimes.Add(visitingTimeFrac);
                        if (!DomainVisitStats.PageStats.ContainsKey(targetDomain))
                        {
                            DomainVisitStats.PageStats[targetDomain] = new PageStats(); 
                        }
                        DomainVisitStats.PageStats[targetDomain].PurchasedUsers++;
                    }

                    //if (this.EntityDictionary[userId].Document["browsing_statistics"] == null)
                    //{
                    var browsingStats = new UserBrowsingStats();
                    browsingStats.BrowsingTime = (int)completeBrowsingDuration.TotalSeconds;
                    browsingStats.DomainChanges = (int)domainChanges;
                    browsingStats.TargetSiteTime = (int)timeSpentOnTargetDomain.TotalSeconds;
                    browsingStats.TargetSiteVisits = targetDomainVisits;
                    browsingStats.TargetSiteDomainTransitionDuration = (int)targetDomainTransitionDuration.TotalSeconds;
                    browsingStats.TargetSiteDomainTransitions = targetDomainTransitionCount;
                    browsingStats.TimeOnMobileSites = timeSpentOnMobileSites.TotalSeconds;
                    browsingStats.WeekendVisits = weekendDomainVisits;

                    var dictKey = userPair.Key.ToString();
                    var document = this.EntityDictionary[dictKey].GetDocument();
                    document["browsing_statistics"] = browsingStats.ToBsonDocument();
                    //document["is_paying"] = userIsPaying ? 1 : 0;
                    //} 
                    //aDomainVisitsTotal += domainChanges;
                }
                _longestVisitPurchaseDuration = spentTimes.Count ==0 ? 0 : spentTimes.Max();
                return _longestVisitPurchaseDuration.Value;
            }
        }



        /// <summary>
        /// Gets the average time for a page to lead to a given target page
        /// </summary>
        /// <param name="events"></param>
        /// <param name="targetHostname"></param>
        /// <returns></returns>
        public TimeSpan GetAveragePageTransitionTime(string fromHost, string toHost)
        {
            fromHost = fromHost.ToHostname(true);
            toHost = toHost.ToHostname(true);
            if (DomainVisitStats[fromHost] == null)
            {
                return TimeSpan.Zero;
            }
            else
            {
                var fref = DomainVisitStats[fromHost].GetFollowingReference(toHost);
                if (fref != null)
                {
                    return fref.TransitionDurationAverage;
                }
                else
                {
                    return TimeSpan.Zero;
                }
            }
            //            string lastHostname = null;
            //            BsonValue lastPage = null;
            //            var totalPageTransitionTime = TimeSpan.Zero;
            //            var transitionsMade = 1;
            //            for (var i = 0; i < events.Count; i++)
            //            {
            //                var crEvent = events[i];
            //                var crHost = crEvent["value"].ToString().ToHostname(true);
            //
            //                //If we came from another page, onto ebag
            //                if (lastHostname != null && !lastHostname.Contains(targetHostname) && crHost.Contains(targetHostname))
            //                {
            //                    //The time it took to get to ebag
            //                    var pageTransitionTime = GetPeriod(lastPage["ondate"], crEvent);
            //                    totalPageTransitionTime = totalPageTransitionTime.Add(pageTransitionTime);
            //                }
            //                else
            //                {
            //                    lastPage = crEvent;
            //                }
            //                lastHostname = crHost;
            //            }
            //            double avgMs = totalPageTransitionTime.Milliseconds / (double)transitionsMade;
            //            return TimeSpan.FromMilliseconds(avgMs);
        }


        public static TimeSpan GetPeriod(BsonValue start, BsonValue end)
        {
            var dt1 = DateTime.Parse(start.AsString);
            var dt2 = DateTime.Parse(end.AsString);
            return dt2 - dt1;
        }

        /// <summary>
        /// Gets the time spent on a site from all visits, using a timespan which ignores irrelevant time.
        /// </summary>
        /// <param name="visits"></param>
        /// <param name="realisticTimeSpan"></param>
        /// <returns></returns>
        public static TimeSpan GetVisitsTimeSpan(BsonArray visits, TimeSpan realisticTimeSpan)
        {
            var dailyPeriodSum = GetDailyPeriodSum(visits);
            double mxSeconds = Math.Max(1, realisticTimeSpan.TotalSeconds);
            return TimeSpan.FromSeconds(dailyPeriodSum.TotalSeconds / mxSeconds);
        }

        /// <summary>
        /// Sums the time span for each day of the visits, ignoring the time from the last visit every day, to the first visit on the next day.
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        public static TimeSpan GetDailyPeriodSum(BsonArray events)
        {
            var span = new TimeSpan();
            if (events.Count == 0)
            {
                return TimeSpan.Zero;
            }
            DateTime lastDayDateTime = DateTime.Parse(events[0]["ondate"].ToString());
            DateTime lastDomainVisitStart = lastDayDateTime;
            DateTime? previousVisitDate = null;
            string lastDomain = events[0]["value"].ToString().ToHostname(true).ToLower();

            var periodAccounted = TimeSpan.Zero;
            for (var i = 1; i < events.Count; i++)
            {
                var ev = events[i];
                var currentEventDate = DateTime.Parse(ev["ondate"].ToString());
                var crDomain = ev["value"].ToString().ToHostname(true).ToLower();
                //Wait for the domain to change
                if (crDomain == lastDomain)
                {

                }
                else
                {
                    //If the last domain visit was on a different day
                    if (lastDayDateTime.Day == currentEventDate.Day)
                    {
                        var periodDiff = (previousVisitDate == null)
                            ? TimeSpan.FromSeconds(1)
                            : previousVisitDate - lastDomainVisitStart;
                        if (periodDiff.Value.TotalSeconds == 0) periodDiff = TimeSpan.FromSeconds(1);
                        periodAccounted += periodDiff.Value;
                    }
                    else
                    {
                        //Domain changed, but on the next day
                        var periodDiff = (previousVisitDate == null)
                            ? TimeSpan.FromSeconds(1)
                            : previousVisitDate - lastDomainVisitStart;
                        if (periodDiff.Value.TotalSeconds == 0) periodDiff = TimeSpan.FromSeconds(1);
                        periodAccounted += periodDiff.Value;
                    }

                    lastDomainVisitStart = currentEventDate;
                }
                previousVisitDate = currentEventDate;
                lastDomain = crDomain;
            }
            if (events.Count == 1)
            {
                span += TimeSpan.FromSeconds(1);
            }
            else
            {
                var periodDiff = previousVisitDate - lastDomainVisitStart;
                if (periodDiff.Value.TotalSeconds == 0) periodDiff = TimeSpan.FromSeconds(1);
                periodAccounted += periodDiff.Value;
            }
            span += periodAccounted;
            return span;
        }

        public static TimeSpan GetPeriod(BsonArray array)
        {
            if (array.Count == 0) return TimeSpan.Zero;
            var start = array[0];
            var end = array[array.Count - 1];
            return DateTime.Parse(end["ondate"].ToString()) - DateTime.Parse(start["ondate"].ToString());
        }

        /// <summary>
        /// Gets the nearest previous page.
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="currentIndex"></param>
        /// <returns></returns>
        public static BsonValue GetPreviousPage(BsonArray elements, int currentIndex)
        {
            if (currentIndex <= 1) return null;
            var crPageHost = elements[currentIndex]["value"].ToString().ToHostname();
            for (var i = (currentIndex - 1); i >= 0; i--)
            {
                var elem = elements[i];
                var host = elem["value"].ToString().ToHostname();
                //If host is diff
                if (crPageHost != host)
                {
                    return elem;
                }
            }
            return null;
        }

        /// <summary>
        /// AgeGroups
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="pageLeadingToTarget"></param>
        /// <param name="targetPage">The page to compare with (q)</param>
        /// <returns></returns>
        public double GetPageRating(string pageLeadingToTarget, string targetPage)
        {
            var crRating = DomainVisitStats.GetRating(pageLeadingToTarget, targetPage);
            if (crRating != null)
            {
                return crRating.Value;
            }

            var targetPageHost = targetPage.ToHostname(true);
            var p = pageLeadingToTarget.ToHostname(true);
            var q = targetPageHost;
            Func<double, double> mx1 = (x) => Math.Max(1, x);
            double decay = 0;
            double weight = 0;
            double traffic = 0;

            double usersSentToQ = DomainVisitStats.GetHostVisitorsCount(q);
            double usersSentToP = DomainVisitStats.GetHostVisitorsCount(p);

            double usersBoughtStuffInQ = DomainVisitStats.Get(q, x => x.PurchasedUsers);
            double payingUsersSentFromPToQ = 0;
            if (DomainVisitStats[p] != null)
            {
                var fref = DomainVisitStats[p].GetFollowingReference(q);
                if (fref != null)
                {
                    payingUsersSentFromPToQ = fref.PurchasedUsers;
                }
            }

            double avgTransition = GetAveragePageTransitionTime(pageLeadingToTarget, targetPage).TotalSeconds;

            traffic = usersSentToQ / mx1(usersSentToP);
            weight = usersBoughtStuffInQ / mx1(payingUsersSentFromPToQ);
            decay = 1.0d / mx1(avgTransition); //transition in ms
            double rating = traffic * weight * decay;
            DomainVisitStats.AddRating(pageLeadingToTarget, targetPage, rating);
            return rating;
        }

        /// <summary>
        /// Gets the average of ratings of pages leading to a target site
        /// </summary>
        /// <param name="siteVisits"></param>
        /// <param name="targetSite"></param>
        /// <returns></returns>
        public double GetAveragePageRating(IEnumerable<IGrouping<string, BsonValue>> siteVisits, string targetSite, bool reset = true)
        {
            if (reset)
            {
                DomainVisitStats.ResetRating();
            }

            var ratings = new List<double>();
            foreach (var visitSet in siteVisits)
            {
                var domain = visitSet.Key;
                if (domain.Contains(targetSite.ToHostname())) continue;
                var rating = GetPageRating(domain, targetSite);
                ratings.Add(rating);
            }
            return ratings.Count == 0 ? 0 : ratings.Average();
        }


        #region "Demographics"
        /// <summary>
        /// Gets the users which are with the same age and gender
        /// </summary>
        /// <param name="age"></param>
        /// <param name="gender"></param>
        /// <returns></returns>
        public IEnumerable<IntegratedDocument> GetUsersWithSameAgeAndGender(int age, string gender)
        {
            var sage = age.ToString();
            var key = $"{gender}_{sage}";
            if (AgeAndGenderGroups.ContainsKey(key))
            {
                return AgeAndGenderGroups[key];
            }
            else
            {
                return (new List<IntegratedDocument>());
            }
        }

        /// <summary>
        /// Todo: maybe cache?
        /// </summary>
        /// <param name="age"></param>
        /// <returns></returns>
        public IEnumerable<IntegratedDocument> GetUsersWithSameAge(int age)
        {
            string sage = age.ToString();
            if (AgeGroups.ContainsKey(sage))
            {
                return AgeGroups[sage];
            }
            return (new List<IntegratedDocument>());
        }
        /// <summary>
        /// Todo: maybe cache?
        /// </summary>
        /// <param name="age"></param>
        /// <returns></returns>
        public IEnumerable<IntegratedDocument> GetBuyersWithSameAge(int age)
        {
            string sage = age.ToString();
            if (AgeGroupsPaying.ContainsKey(sage))
            {
                return AgeGroupsPaying[sage];
            }
            return (new List<IntegratedDocument>());
        }

        public IEnumerable<IntegratedDocument> GetBuyersWithSameGender(string gender)
        {

            if (GenderGroupsPaying.ContainsKey(gender))
            {
                return GenderGroupsPaying[gender];
            }
            return (new List<IntegratedDocument>());
        }

        public IEnumerable<IntegratedDocument> GetusersWithSameGender(string gender)
        {
            if (GenderGroups.ContainsKey(gender))
            {
                return GenderGroups[gender];
            }
            return (new List<IntegratedDocument>());
        }
        #endregion

        public int GetEntityCount()
        {
            return EntityDictionary.Count;
        }

        public IEnumerable<IntegratedDocument> TargetDomainVisitors(IEnumerable<IntegratedDocument> records)
        {
            return records;
            //            foreach (var doc in records)
            //            {
            //                if (!doc.Document.Contains("browsing_statistics"))
            //                {
            //                    continue;
            //                }
            //                var bsonValue = doc.Document["browsing_statistics"] as BsonDocument;
            //                if (bsonValue.Contains("targetSiteVisits") && bsonValue["targetSiteVisits"].AsInt64 > 0)
            //                {
            //                    yield return doc;
            //                } 
            //            }
        }
        /// <summary>
        /// Gets the average of domain sessions / user count
        /// </summary>
        /// <returns></returns>
        public double GetAverageDomainVisits()
        {
            // 8 - 12h
            var transitions = (double)this.DomainVisitStats.DomainTransitionsTotal();
            return transitions / Math.Max(1, GetEntityCount());
            //return (double)DomainVisitsTotal / Math.Max(1, GetEntityCount());
        }

        public int GetDomainVisits(string target)
        {
            target = target.ToHostname();
            var targetStats = DomainVisitStats[target];
            int visits = targetStats == null ? 0 : targetStats.GetTotalTransitionCount();
            return visits;
        }
        public int GetDomainVisitors(string target)
        {
            target = target.ToHostname();
            var targetStats = DomainVisitStats[target];
            int visits = targetStats == null ? 0 : targetStats.GetUsersVisitedCount();
            return visits;
        }

        public int GetDomainVisits(string userId, string target)
        {
            target = target.ToHostname();
            var targetStats = DomainVisitStats[target];
            int visits = targetStats == null ? 0 : targetStats.GetTotalTransitionCount();
            return visits;
        }

        public void AddBuyVisitValue(double probBuyVidit)
        {
            BuyVisitValues += probBuyVidit;
        }
        public Dictionary<string, IEnumerable<PageStats>> HighrankPageCache { get; set; }
        private object _highrankingPagesLock;
        public IEnumerable<PageStats> GetHighrankingPages(string targetDomain, int atMost)
        {
            lock (_highrankingPagesLock)
            {
                if (HighrankPageCache.ContainsKey(targetDomain))
                {
                    return HighrankPageCache[targetDomain];
                }
                else
                {
                    try
                    {
                        var values = DomainVisitStats.PageStats
                            .Select(x => x.Value)
                            .OrderByDescending(x =>
                            {
                                var score = x.GetTargetRating(targetDomain);
                                return score?.Value ?? 0;
                            })
                            .Take(atMost).ToList();
                        HighrankPageCache[targetDomain] = values;
                        return values;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        return new List<PageStats>();
                    }

                }
            }
        }

        public string GetBsonString(BsonDocument val, string k, string defaultVal = "0")
        {
            if (!val.Contains(k)) return defaultVal;
            var kv = val[k].ToString();
            if (kv.Length == 0) return defaultVal;
            return kv;
        }
        public void GroupDemographics(bool regroup = false)
        {
            lock (_demographyLock)
            {
                if (_grouped && !regroup) return;
                this.DistinctUsers = EntityDictionary
                    .GroupBy(x =>
                    {
                        var valueDocument = x.Value.GetDocument();
                        return valueDocument["uuid"];
                    })
                    .ToDictionary(x => x.Key.ToString(), y => y.Select(x => x.Value).ToList());

                this.GenderGroups = DistinctUsers
                    .SelectMany(x => x.Value)
                    .GroupBy(x => GetBsonString(x.GetDocument(), "gender"))
                    .ToDictionary(x => x.Key, x => x
                        .Where(y =>
                        { 
                            //TODO: Move this..
                            var argDocument = y.GetDocument();
                            var bsonValue = argDocument.Contains("browsing_statistics") ? (argDocument["browsing_statistics"] as BsonDocument) : null;
                            return bsonValue==null ? false : bsonValue.Contains("targetSiteVisits") && bsonValue["targetSiteVisits"].AsInt64 > 0;
                        })
                        .ToList());
                this.GenderGroupsPaying = DistinctUsers
                    .SelectMany(x => x.Value)
                    .GroupBy(x => GetBsonString(x.GetDocument(), "gender"))
                    .ToDictionary(x => x.Key, x => x
                        .Where(y =>
                        { 
                            //TODO: Move this..
                            var argDocument = y.GetDocument();
                            var bsonValue = argDocument.Contains("browsing_statistics") ? (argDocument["browsing_statistics"] as BsonDocument) : null;
                            var isPaying = y.Has("is_paying") && y.GetInt("is_paying") == 1;
                            return isPaying && bsonValue ==null ? false : bsonValue.Contains("targetSiteVisits") && bsonValue["targetSiteVisits"].AsInt64 > 0;
                        })
                        .ToList());
                this.AgeAndGenderGroups = DistinctUsers
                    .SelectMany(x => x.Value)
                    .GroupBy(x => $"{GetBsonString(x.GetDocument(), "gender")}_{GetBsonString(x.GetDocument(), "age")}")
                    .ToDictionary(x => x.Key, x => x
                        .Where(y =>
                        {
                            //TODO: Move this..
                            var argDocument = y.GetDocument();
                            var bsonValue = argDocument.Contains("browsing_statistics") ? (argDocument["browsing_statistics"] as BsonDocument) : null;
                            return bsonValue == null ? false : bsonValue.Contains("targetSiteVisits") && bsonValue["targetSiteVisits"].AsInt64 > 0;
                        })
                        .ToList());

                this.AgeGroups = DistinctUsers
                    .SelectMany(x => x.Value)
                    .GroupBy(x => GetBsonString(x.GetDocument(), "age"))
                    .ToDictionary(x => x.Key, x => x
                        .Where(y =>
                        { 
                            var argDocument = y.GetDocument();
                            var bsonValue = argDocument.Contains("browsing_statistics") ? (argDocument["browsing_statistics"] as BsonDocument) : null;
                            return bsonValue != null && (bsonValue.Contains("targetSiteVisits") && bsonValue["targetSiteVisits"].AsInt64 > 0);
                        })
                        .ToList());
                this.AgeGroupsPaying = DistinctUsers
                    .SelectMany(x => x.Value)
                    .GroupBy(x => GetBsonString(x.GetDocument(), "age"))
                    .ToDictionary(x => x.Key, x => x
                        .Where(y =>
                        {
                            var argDocument = y.GetDocument();
                            var bsonValue = argDocument.Contains("browsing_statistics") ? (argDocument["browsing_statistics"] as BsonDocument) : null;
                            var isPaying = y.Has("is_paying") && y.GetInt("is_paying") == 1;
                            return isPaying && (bsonValue != null && (bsonValue.Contains("targetSiteVisits") && bsonValue["targetSiteVisits"].AsInt64 > 0));
                        })
                        .ToList());
                _grouped = true;
            }
        }
        public void AddRatingFeature(byte type, string uuid, string key_value)
        {
            _lock.EnterWriteLock();
            if (!_featureKeyDictionary.ContainsKey(type))
            {
                _featureKeyDictionary[type] = new Dictionary<string, Score>();
            }
            if (!_featureKeyDictionary[type].ContainsKey(key_value))
            {
                _featureKeyDictionary[type][key_value] = new Score();
            }
            _featureKeyDictionary[type][key_value] += 1;


            if (!_usersWithSpecialEvents.ContainsKey(uuid))
            {
                _usersWithSpecialEvents[uuid] = new Dictionary<byte, HashSet<string>>();
            }
            if (!_usersWithSpecialEvents[uuid].ContainsKey(type))
            {
                _usersWithSpecialEvents[uuid][type] = new HashSet<string>();
            }
            _usersWithSpecialEvents[uuid][type].Add(key_value);
            if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
        }

        public IEnumerable<Score<string>> GetTopRatedFeatures(byte type, int atMost = 10)
        {
            var sortedFeatures = _featureKeyDictionary[type].OrderBy(x=>x.Key);
            return sortedFeatures.Select(x=> new Score<string>(x.Value)
            {
                Value = x.Key
            }).Take(atMost);
        }

        public IEnumerable<int> GetTopRatedFeatures(string userId, byte featureId, int atMost = 10)
        {
            var sortedFeatures = this.GetTopRatedFeatures(featureId, atMost).ToArray();
            for (var i = 0; i < atMost; i++)
            {
                Score<string> feature = i>= sortedFeatures.Length ? null : sortedFeatures[i];
                var value = 0;
                if (feature!=null && _usersWithSpecialEvents.ContainsKey(userId) && _usersWithSpecialEvents[userId].ContainsKey(featureId))
                {
                    value = _usersWithSpecialEvents[userId][featureId].Contains(feature.Value) ? 1 : 0;
                }                
                yield return value;
            }
        }
    }
}