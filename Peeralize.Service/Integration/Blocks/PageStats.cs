using System;
using System.Collections.Generic;
using nvoid.extensions;

namespace Peeralize.Service.Integration.Blocks
{
    public class PageStats
    {
        public string PageHost => Strings.ToHostname(Page);
        public long UsersVisitedTotal { get; set; }

        /// <summary>
        /// The number of times this hostname was visited
        /// </summary>
        public long VisitsTotal { get; set; }
        public string Page { get; set; }
        public int PurchasedUsers { get; set; }
        /// <summary>
        /// The domains to which this page leads, and information about them
        /// </summary>
        public Dictionary<string, PageStats> FollowingReferences { get; private set; }

        public TimeSpan TransitionDuration { get; set; }
        public int TransitionsCount { get; set; }

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
        public TimeSpan TransitionDurationAverage => TransitionsCount == 0
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds(TransitionDuration.Seconds / TransitionsCount);

        public PageStats()
        {
            FollowingReferences = new Dictionary<string, PageStats>();
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
    }
}