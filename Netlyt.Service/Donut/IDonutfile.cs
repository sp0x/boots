namespace Netlyt.Service.Donut
{
    public interface IDonutfile
    {
        void SetupCacheInterval(long cacheInterval);
        bool ReplayInputOnFeatures { get; set; }

        bool SkipFeatureExtraction { get; set; }
    }
}