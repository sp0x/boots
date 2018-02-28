using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using nvoid.extensions;
using Netlyt.Service.Models;

namespace Netlyt.ServiceTests.Netinfo
{
    /// <summary>
    /// Stats for multiple pages
    /// </summary>
    public class CrossPageStats
    {
        /// <summary>
        /// 
        /// </summary>
        public ConcurrentDictionary<string, PageStats> PageStats { get; set; }
        
        public CrossPageStats()
        {
            PageStats = new ConcurrentDictionary<string, PageStats>();
        }

        public long DomainVisitsTotal()
        {
            long outx = 0;
            foreach (var page in PageStats)
            {
                outx += page.Value.PageVisitsTotal;
            }
            return outx;
        }
        private long? transitionsTotalCache = null;
        /// <summary>
        /// The total number of times that domains have been changed.
        /// (Users passing from one domain to another)
        /// </summary>
        /// <returns></returns>
        public long DomainTransitionsTotal()
        {
            if (transitionsTotalCache != null) return transitionsTotalCache.Value;
            int transitionsTotal = 0;
            foreach (var dom in PageStats)
            {
                transitionsTotal += dom.Value.GetTotalTransitionCount();
            }
            transitionsTotalCache = transitionsTotal;
            return transitionsTotal;
        }
        /// <summary>
        /// Gets a value from a given domain's stats
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="getter"></param>
        /// <returns></returns>
        public double Get(string hostname , Func<PageStats, double> getter)
        {
            if (this.PageStats.ContainsKey(hostname))
            {
                return getter(this.PageStats[hostname]);
            }
            else
            {
                return 0;
            }
        }
        public bool ContainsPage(string page)
        {
            return PageStats.ContainsKey(page);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageStats"></param>
        public void AddPage(string page, PageStats pageStats)
        {
            PageStats[page]= pageStats;
        }

        public PageStats this[string key] 
        {
            get { return PageStats.ContainsKey(key) ? PageStats[key] : null; }
            set
            {
                PageStats[key] = value;
            }
        }
         
        /// <summary>
        /// The number of sessions for the target domain
        /// </summary>
        /// <param name="targetPage"></param>
        /// <returns></returns>
        public long GetVisitsCount(string targetPage)
        {
            var pagestat = this[targetPage];
            if (pagestat != null)
            {
                return pagestat.GetTotalTransitionCount();
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// Gets the number of users visited the target page
        /// </summary>
        /// <param name="targetPage"></param>
        /// <returns></returns>
        public long GetHostVisitorsCount(string targetPage)
        {
            var hostname = targetPage.ToHostname(true);
            var matchingHosts = Enumerable.Select<KeyValuePair<string, PageStats>, PageStats>(PageStats.Where(x => Strings.ToHostname(x.Key, true).ToLower().Equals(hostname)), x => x.Value);
            long count = 0;
            foreach (var page in matchingHosts)
            {
                count += page.GetUsersVisitedCount();
            }
            return count;
        }
        /// <summary>
        /// Adds rating
        /// </summary>
        /// <param name="pageLeadingToTarget"></param>
        /// <param name="targetPage"></param>
        /// <param name="rating"></param>
        public void AddRating(string pageLeadingToTarget, string targetPage, double rating)
        {
            if (!PageStats.ContainsKey(pageLeadingToTarget))
            {
                this.PageStats[pageLeadingToTarget] = new PageStats();
            }
            this.PageStats[pageLeadingToTarget].AddRating(targetPage, rating);
        }
        public void SetRating(string pageLeadingToTarget, string targetPage, double rating)
        {
            if (!PageStats.ContainsKey(pageLeadingToTarget))
            {
                this.PageStats[pageLeadingToTarget] = new PageStats();
            }
            this.PageStats[pageLeadingToTarget].SetRating(targetPage, rating);
        }

        public Score GetRating(string pageLeadingToTarget, string targetPage)
        {
            if (!PageStats.ContainsKey(pageLeadingToTarget))
            {
                return null;
            }
            else
            {
                return this.PageStats[pageLeadingToTarget].GetTargetRating(targetPage);
            }
        }
        public void ResetRating()
        {
            foreach (var pagest in PageStats)
            {
                pagest.Value.ResetRating();
            }
        }

        public void AddDomainVisit(string userKey, string domain, TimeSpan visitDuration)
        {
            if (!PageStats.ContainsKey(domain))
            {
                PageStats[domain] = new PageStats(); 
            }
            PageStats[domain].AddVisit(userKey, visitDuration);
        }
    }
}