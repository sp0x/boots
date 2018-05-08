using System;
using Donut.Data;
using Donut.Integration;
using Netlyt.Interfaces;

namespace Donut
{
    public interface IDonutContext
    {
        IApiAuth ApiAuth { get; }
        int CacheRunInterval { get; }
        IRedisCacher Database { get; }
        IIntegration Integration { get; set; }
        string Prefix { get; set; }

        void Cache(bool force = false);
        void CacheAndClear(bool force = false);
        void Complete();
        void Dispose();
        IDataSet GetOrAddDataSet(ICacheSetSource source, Type type);
        double MetaEntityMax(string key, int category, double @default);
        void SetCacheRunInterval(int interval);
        void TruncateMeta(int metaSpentTime);
        void TruncateSets();
    }
}