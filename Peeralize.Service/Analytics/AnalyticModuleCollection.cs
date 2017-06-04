using System.Collections.Generic; 

namespace Peeralize.Service.Analytics
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