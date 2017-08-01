using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MongoDB.Bson;
using nvoid.extensions;

namespace Peeralize.Service.Integration.Blocks
{
    public class CrossSiteAnalyticsHelper
    {
        public Dictionary<object, IntegratedDocument> EntityDictionary { get; private set; }
        public CrossPageStats PaginationStatus { get; private set; }


        public CrossSiteAnalyticsHelper(Dictionary<object, IntegratedDocument> sessions,
            CrossPageStats paginationStatus)
        {
            EntityDictionary = sessions;
            this.PaginationStatus = paginationStatus;
        }

        /// <summary>
        /// Gets the max user time(secs) spent on the target domain, out of all the user's browsing times.
        /// Using: max(time_on_target_domain / time_in_all_domains) in seconds.
        /// Using this method records user browsing information in this format:
        ///  browsing_statistics = { browsingTime : seconds , targetSiteTime: seconds , targetSiteTransitionAverage : seconds}
        ///  isPaying = bool
        /// </summary>
        /// <param name="targetDomain"></param>
        /// <returns></returns>
        public double GetLongestVisitPurchaseDuration(string targetDomain, string specialUrl)
        {
            var spentTimes = new List<double>();
            
            foreach (var userPair in this.EntityDictionary)
            {
                var userId = userPair.Key;
                var visits = (BsonArray)userPair.Value.Document["events"]; 
                var userIsPaying = false;
                //var userInternetBrowsingTime = TimeSpan.Zero;

                var firstVisit = visits[0];
                var lastDomain = firstVisit["value"].ToString().ToHostname().ToLower();
                var userTargetleadingHosts = new HashSet<string>();
                
                var lastDomainSessionStart = DateTime.Parse(firstVisit["ondate"].ToString());
                var lastDomainVisitDuration = TimeSpan.Zero;
                var completeBrowsingDuration = TimeSpan.Zero;
                var timeSpentOnTargetDomain = TimeSpan.Zero;
                var targetDomainVisits = 0;
                var targetDomainTransitionDuration = lastDomainVisitDuration;
                var targetDomainTransitionCount = 0; 

                for (int i = 1; i < visits.Count; i++)
                {
                    var visit = visits[i];
                    var currentUrl = visit["value"].ToString();
                    var crDomain = currentUrl.ToHostname().ToLower();
                    var visited = DateTime.Parse(visit["ondate"].ToString());

                    if (currentUrl.Contains(specialUrl))
                    {
                        userIsPaying = true;
                    }
                    
                    if (crDomain == lastDomain){ 
                        //We're still in the same domain, nothing has changed
                    }
                    else
                    {
                        //Domain has changed, add the time from the last domain
                        var visitDuration = visited - lastDomainSessionStart;
                        //Add at least 2 seconds if duration is 0
                        if (visitDuration.TotalSeconds == 0) visitDuration = visitDuration.Add(TimeSpan.FromSeconds(1));

                        completeBrowsingDuration += visitDuration;
                        


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

                                PaginationStatus.PageStats[lastDomain].AddFollowingSite(targetDomain);
                                var followingReference = PaginationStatus.PageStats[lastDomain].FollowingReferences[crDomain];
                                //If the current user has not marked that he has visited the site, leading to the targetDomain
                                if (!userTargetleadingHosts.Contains(lastDomain))
                                {
                                    followingReference.UsersVisitedTotal++;
                                    //Increase the paying members, which were led
                                    if (userIsPaying)
                                    {
                                        followingReference.PurchasedUsers++;
                                    }
                                    userTargetleadingHosts.Add(lastDomain);
                                }
                                followingReference.TransitionDuration += lastDomainVisitDuration;
                                followingReference.TransitionsCount++;
                                //Add the tranition domain
                            }
                        }
                        
                        //Set our last domain visit to this current visit
                        lastDomainSessionStart = visited;
                        lastDomainVisitDuration = visitDuration;
                    }
                    lastDomain = crDomain;
                }

                if (userIsPaying)
                {
                    var visitingTimeFrac = (double)(completeBrowsingDuration.TotalSeconds == 0 ?
                        0 : (timeSpentOnTargetDomain.TotalSeconds / completeBrowsingDuration.TotalSeconds));
                    spentTimes.Add(visitingTimeFrac);
                    PaginationStatus.PageStats[targetDomain].PurchasedUsers++;
                }


                //if (this.EntityDictionary[userId].Document["browsing_statistics"] == null)
                //{
                var browsingStats = new UserBrowsingStats();
                browsingStats.BrowsingTime = (int)completeBrowsingDuration.TotalSeconds;
                browsingStats.TargetSiteTime = (int)timeSpentOnTargetDomain.TotalSeconds;
                browsingStats.TargetSiteVisits = targetDomainVisits;
                browsingStats.TargetSiteDomainTransitionDuration = (int)targetDomainTransitionDuration.TotalSeconds;
                browsingStats.TargetSiteDomainTransitions = targetDomainTransitionCount;
                this.EntityDictionary[userId].Document["browsing_statistics"] = browsingStats.ToBsonDocument(); 
                this.EntityDictionary[userId].Document["is_paying"] = userIsPaying ? 1 : 0;
                //} 
            }
            return spentTimes.Max();
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
            if (PaginationStatus[fromHost] == null)
            {
                return TimeSpan.Zero;
            }
            else
            {
                var fref = PaginationStatus[fromHost].GetFollowingReference(toHost);
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
                if (crPageHost!=host)
                {
                    return elem;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="pageLeadingToTarget"></param>
        /// <param name="targetPage"></param>
        /// <returns></returns>
        public double GetPageRating(string pageLeadingToTarget, string targetPage)
        {
            var targetPageHost = targetPage.ToHostname(true);
            var p = pageLeadingToTarget.ToHostname(true);
            var q = targetPageHost;
            Func<double, double> mx1 = (x) => Math.Max(1, x);
            double decay = 0;
            double weight = 0;
            double traffic = 0; 

            double usersSentToQ = PaginationStatus.GetVisitorsCount(q);
            double usersSentToP = PaginationStatus.GetVisitorsCount(p);
            double usersBoughtStuffInQ = PaginationStatus[q]!=null ? 
                PaginationStatus[q].PurchasedUsers : 0;
            double payingUsersSentFromPToQ = 0;
            if (PaginationStatus[p] != null)
            {
                var fref = PaginationStatus[p].GetFollowingReference(q);
                if (fref != null)
                {
                    payingUsersSentFromPToQ = fref.PurchasedUsers;
                }
            }

            double avgTransition = GetAveragePageTransitionTime(pageLeadingToTarget, targetPage).Seconds;

            traffic = usersSentToQ / mx1(usersSentToP);
            weight = usersBoughtStuffInQ / mx1(payingUsersSentFromPToQ);
            decay = 1.0d / mx1(avgTransition); //transition in ms

            return traffic * weight * decay;
        }

        /// <summary>
        /// Gets the average of ratings of pages leading to a target site
        /// </summary>
        /// <param name="siteVisits"></param>
        /// <param name="targetSite"></param>
        /// <returns></returns>
        public double GetAveragePageRating(IEnumerable<IGrouping<string, BsonValue>> siteVisits, string targetSite)
        {
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
    }
}