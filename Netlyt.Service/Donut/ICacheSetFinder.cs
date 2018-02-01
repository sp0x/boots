using System.Collections.Generic;
using nvoid.db.Caching;

namespace Netlyt.Service.Donut
{
    public interface ICacheSetFinder
    { 
        IReadOnlyList<CacheSetProperty> FindSets(DonutContext context);
    }
}