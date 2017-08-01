using System;
using System.Collections.Generic;
using System.Linq;
using nvoid.extensions;

namespace Peeralize.Service
{
    public class CrossPageStats
    {
        public Dictionary<string, PageStats> PageStats { get; set; }

        public CrossPageStats()
        {
            PageStats = new Dictionary<string, Service.PageStats>();
        }

        public bool ContainsPage(string page)
        {
            return PageStats.ContainsKey(page);
        }

        public void AddPage(string page, PageStats pageStats)
        {
            PageStats.Add(page, pageStats);
        }

        public PageStats this[string key] 
        {
            get { return PageStats.ContainsKey(key) ? PageStats[key] : null; }
            set { PageStats[key] = value; }
        }

        public long GetVisitorsCount(string targetPage)
        {
            var pagestat = this[targetPage];
            if (pagestat != null)
            {
                return pagestat.UsersVisitedTotal;
            }
            else
            {
                return 0;
            }
        }
        public long GetHostVisitorsCount(string targetPage)
        {
            var hostname = targetPage.ToHostname(true);
            var matchingHosts = PageStats.Where(x => x.Key.ToHostname(true).ToLower().Equals(hostname))
                .Select(x => x.Value);
            long count = 0;
            foreach (var page in matchingHosts)
            {
                count += page.UsersVisitedTotal;
            }
            return count;
        }
    }


    public class PageStats
    {
        public string PageHost => Strings.ToHostname(Page);
        public long UsersVisitedTotal { get; set; }
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