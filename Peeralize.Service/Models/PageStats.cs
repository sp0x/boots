using System;
using System.Collections.Generic;
using nvoid.extensions;
using Peeralize.Service.Integration.Blocks;

namespace Peeralize.Service.Models
{
    /// <summary>
    /// Page stats helper
    /// </summary>
    public class PageStats
    {
        public string PageHost => Strings.ToHostname(Page);
//        /// <summary>
//        /// TODO: Remove this, and use 
//        /// </summary>
//        public long UsersVisitedTotal { get; set; }

        /// <summary>
        /// The number of times this hostname was visited, including the times that the user just went to another page.
        /// </summary>
        public long PageVisitsTotal { get; set; }
        public string Page { get; set; }
        public int PurchasedUsers { get; set; }
        /// <summary>
        /// The domains to which this page leads, and information about them
        /// </summary>
        public Dictionary<string, PageStats> FollowingReferences { get; private set; }
        public Dictionary<string, Score> TargetRatings { get; private set; }

        public TimeSpan TotalTransitionDuration { get; set; }
        /// <summary>
        /// The number of times this domain had a session(user arriving, visiting 1 or more pages, and leaving)
        /// </summary>
//        public int TransitionsCount { get; set; }
        public Dictionary<string, PageVisit> UserVisits { get; set; }

        public int GetUsersVisitedCount()
        {
            return UserVisits.Count;
        }
        

        public PageStats GetFollowingReference(string domain)
        {
            if (FollowingReferences.ContainsKey(domain))
            {
                return FollowingReferences[domain];
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// The average time it takes to get to this page, from another one
        /// </summary>
        public TimeSpan TransitionDurationAverage => UserVisits.Count == 0
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds(TotalTransitionDuration.TotalSeconds / GetTotalTransitionCount());


        public PageStats()
        {
            //TotalVisitDuration = TimeSpan.Zero;
            FollowingReferences = new Dictionary<string, PageStats>();
            TargetRatings = new Dictionary<string, Score>();
            UserVisits = new Dictionary<string, PageVisit>();
        }
        /// <summary>
        /// Gets the time spent for all the users
        /// </summary>
        /// <returns></returns>
        public double GetTotalVisitDuration()
        {
            var outd = 0.0d;
            foreach (var visit in UserVisits)
            {
                var duration = visit.Value.Duration;
                outd += duration.TotalSeconds;
            }
            return outd;
        }
        /// <summary>
        /// The time a user has spent on this domain
        /// </summary>
        /// <param name="userKey"></param>
        /// <returns></returns>
        public double GetUserVisitDuration(string userKey)
        {
            var outd = 0.0d;
            if (UserVisits.ContainsKey(userKey))
            {
                outd = UserVisits[userKey].Duration.TotalSeconds;
            }
            return outd;
        }

        public int GetTotalTransitionCount()
        {
            int outi = 0;
            foreach (var visit in UserVisits)
            {
                outi += visit.Value.Transitions;
            }
            return outi;
        }

        /// <summary>
        /// The average duration of a session on this site
        /// </summary>
        /// <returns></returns>
        public double GetAverageVisitDuration()
        {
            var avg = GetTotalVisitDuration() / Math.Max(1.0d, GetTotalTransitionCount());
            return avg;
        }
        /// <summary>
        /// The average duration of a user's session on this site
        /// </summary>
        /// <param name="userKey"></param>
        /// <returns></returns>
        public double GetUserAverageVisitDuration(string userKey)
        {
            if (UserVisits.ContainsKey(userKey))
            {
                var avg = UserVisits[userKey].Duration.TotalSeconds / UserVisits[userKey].Transitions;
                return avg;
            }
            return 0.0d;
        }

        public bool AddFollowingSite(string domain)
        {
            if (!FollowingReferences.ContainsKey(domain))
            {
                var stats = new PageStats();
                stats.Page = domain; 
                FollowingReferences.Add(domain, stats);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the rating of this page, with respect to another page.
        /// </summary>
        /// <param name="targetPage"></param>
        /// <param name="rating"></param>
        public void SetRating(string targetPage, double rating)
        {
            if (!TargetRatings.ContainsKey(targetPage))
            {
                TargetRatings[targetPage] = new Score();
            } 
            TargetRatings[targetPage] = new Score(rating);
        }
        /// <summary>
        /// Sets the rating of this page, with respect to another page.
        /// </summary>
        /// <param name="targetPage"></param>
        /// <param name="rating"></param>
        public void AddRating(string targetPage, double rating)
        {
            if (!TargetRatings.ContainsKey(targetPage))
            {
                TargetRatings[targetPage] = new Score();
            }
            TargetRatings[targetPage] += rating; 
        }
        /// <summary>
        /// Gets the rating for this page, with respect to the given target page
        /// </summary>
        /// <param name="targetPage"></param>
        /// <returns></returns>
        public Score GetTargetRating(string targetPage)
        {
            if (!TargetRatings.ContainsKey(targetPage))
            {
                //TargetRatings[targetPage] = new Score();
                return null;
            }
            return TargetRatings[targetPage];
        }

        public void ResetRating()
        {
            TargetRatings.Clear();
        }

        public void AddVisit(string userKey, TimeSpan visitDuration)
        {
            if (visitDuration.TotalSeconds < 0) visitDuration += TimeSpan.FromSeconds(1);
            if (!UserVisits.ContainsKey(userKey))
            {
                UserVisits[userKey] = new PageVisit(visitDuration);
            }
            else UserVisits[userKey].Add(visitDuration);
        }
    }
}