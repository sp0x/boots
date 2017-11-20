using System.Collections.Generic; 

namespace Netlyt.Service.Analytics
{
    public class AnalyticModuleCollection
    {
        private IEnumerable<AnalyticModule> Items { get; set; }

        public AnalyticModuleCollection()
        {
            Items = new List<AnalyticModule>();
        }
    }
}