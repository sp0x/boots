using System.Collections.Generic;
using Donut.Caching;

namespace Donut
{
    public interface ICacheSetFinder
    { 
        IReadOnlyList<CacheSetProperty> FindSets(DonutContext context);
        IReadOnlyList<DataSetProperty> FindDataSets(DonutContext context);
    }
}