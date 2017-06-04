namespace Peeralize.Service.Analytics
{
    public class AnalyticsCore
    {
        public AnalyticModuleCollection Modules { get; private set; }

        public AnalyticsCore()
        {
            Modules = new AnalyticModuleCollection();


        }
    }
}
