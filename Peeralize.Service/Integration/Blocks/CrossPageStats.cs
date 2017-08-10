using System.Collections.Generic;
using System.Linq;
using nvoid.extensions;
using Peeralize.Service.Integration.Blocks;

namespace Peeralize.Service
{
    /// <summary>
    /// Stats for multiple pages
    /// </summary>
    public class CrossPageStats
    {
        public Dictionary<string, PageStats> PageStats { get; set; }
        
        public CrossPageStats()
        {
            PageStats = new Dictionary<string, PageStats>();
        }

        public long DomainVisitsTotal()
        {
            long outx = 0;
            foreach (var page in PageStats)
            {
                outx += page.Value.VisitsTotal;
            }
            return outx;
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
            var matchingHosts = Enumerable.Select<KeyValuePair<string, PageStats>, PageStats>(PageStats.Where(x => Strings.ToHostname(x.Key, true).ToLower().Equals(hostname)), x => x.Value);
            long count = 0;
            foreach (var page in matchingHosts)
            {
                count += page.UsersVisitedTotal;
            }
            return count;
        }
    }
}