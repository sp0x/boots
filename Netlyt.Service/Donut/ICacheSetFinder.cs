using System.Collections.Generic;
using nvoid.db;
using nvoid.db.Caching;

namespace Netlyt.Service.Donut
{
    public interface ICacheSetFinder
    { 
        IReadOnlyList<CacheSetProperty> FindSets(DonutContext context);
        IReadOnlyList<DataSetProperty> FindDataSets(DonutContext context);
    }
}