using System;
using System.Collections.Generic;
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
        public long GetLongestVisitPurchaseDuration(string targetDomain, string specialUrl)
        {
            var spentTimes = new List<long>();
            
            foreach (var userPair in this.EntityDictionary)
            {
                var userId = userPair.Key;
                var visits = (BsonArray)userPair.Value.Document["events"];
                var userIsPaying = false;
                //var addedPayingReference = false;
                var userInternetBrowsingTime = TimeSpan.Zero;

                var lastDomain = visits[0]["value"].ToString().ToHostname().ToLower();
                var userTargetleadingHosts = new HashSet<string>();
                
                var lastDomainSessionStart = DateTime.Parse(visits[0]["ondate"].ToString());
                var lastDomainVisitDuration = TimeSpan.Zero;
                var completeBrowsingDuration = TimeSpan.Zero;
                var timeSpentOnTargetDomain = TimeSpan.Zero;
                var targetDomainTransitionDuration = lastDomainVisitDuration;
                var targetDomainTransitionCount = 0;

                //var enteredTargetDomain = false;
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
                        completeBrowsingDuration += visitDuration;
                        //If we were on our target domain, and the current one is not any more
                        if (lastDomain == targetDomain)
                        {
                            timeSpentOnTargetDomain += visitDuration;
                            if (lastDomainVisitDuration > TimeSpan.Zero)
                            {
                                targetDomainTransitionCount++;
                            }
                        }
                        else
                        {
                            //If we transitioned from another domain, to our domain
                            if (crDomain == targetDomain)
                            {
                                targetDomainTransitionDuration += lastDomainVisitDuration;

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




                var visitingTimeFrac = timeSpentOnTargetDomain.Seconds / userInternetBrowsingTime.Seconds;
                if (userIsPaying)
                {
                    spentTimes.Add(visitingTimeFrac);
                    PaginationStatus.PageStats[targetDomain].PurchasedUsers++;
                }


                if (this.EntityDictionary[userId].Document["browsing_statistics"] == null)
                {
                    this.EntityDictionary[userId].Document["browsing_statistics"] = new BsonDocument();
                    this.EntityDictionary[userId].Document["browsing_statistics"]["browsingTime"] =
                        userInternetBrowsingTime.Seconds;
                    this.EntityDictionary[userId].Document["browsing_statistics"]["targetSiteTime"] =
                        timeSpentOnTargetDomain.Seconds;
                    double transitionAverage = targetDomainTransitionCount== 0 ? 0 : 
                        (double)targetDomainTransitionDuration.Seconds / (double)targetDomainTransitionCount;
                    this.EntityDictionary[userId].Document["browsing_statistics"]["targetSiteTransitionAverage"] =
                        transitionAverage;
                    this.EntityDictionary[userId].Document["is_paying"] = userIsPaying;
                }

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
            return PaginationStatus[fromHost].FollowingReferences[toHost].TransitionDurationAverage;
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
            return TimeSpan.FromSeconds(dailyPeriodSum.Seconds / realisticTimeSpan.Seconds);
        }

        /// <summary>
        /// Sums the time span for each day of the visits, ignoring the time from the last visit every day, to the first visit on the next day.
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        public static TimeSpan GetDailyPeriodSum(BsonArray events)
        {
            var span = new TimeSpan();
            DateTime lastDayDateTime = DateTime.Parse(events[0]["ondate"].ToString());
            DateTime lastProcessedDate = lastDayDateTime;
            for (var i = 1; i < events.Count; i++)
            {
                var ev = events[i];
                var currentEventDate = DateTime.Parse(ev["ondate"].ToString());
                if (lastDayDateTime.Day == currentEventDate.Day)
                {
                    var diff = currentEventDate - lastProcessedDate;
                    span = span.Add(diff);
                }
                else
                {
                    lastDayDateTime = currentEventDate;
                }
            }
            return span;
        }

        public static TimeSpan GetPeriod(BsonArray array)
        {
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

            var decay = 0;
            var weight = 0;
            long traffic = 0; 

            var usersSentToQ = PaginationStatus.GetVisitorsCount(q);
            var usersSentToP = PaginationStatus.GetVisitorsCount(p);
            var usersBoughtStuffInQ = PaginationStatus[q].PurchasedUsers;
            var payingUsersSentFromPToQ = PaginationStatus[p].FollowingReferences[p].PurchasedUsers;
            var avgTransition = GetAveragePageTransitionTime(pageLeadingToTarget, targetPage);

            traffic = usersSentToQ / usersSentToP;
            weight = usersBoughtStuffInQ / payingUsersSentFromPToQ;
            decay = 1 / avgTransition.Milliseconds; //transition in ms

            return traffic * weight * decay;
        }

        /// <summary>
        /// Gets the average of ratings of pages leading to a target site
        /// </summary>
        /// <param name="siteVisits"></param>
        /// <param name="targetSite"></param>
        /// <returns></returns>
        public object GetAveragePageRating(IEnumerable<IGrouping<string, BsonValue>> siteVisits, string targetSite)
        {
            var ratings = new List<double>();
            foreach (var visitSet in siteVisits)
            {
                var page = visitSet.Key;
                if (page.Contains(targetSite.ToHostname())) continue;
                var rating = GetPageRating(page, targetSite);
                ratings.Add(rating);
            }
            return ratings.Average();
        }
    }
}