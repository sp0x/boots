using System;
using Netlyt.Interfaces;

namespace Donut.Data
{
    public interface ISetCollection
    {
        ICacheSet GetOrAddSet(ICacheSetSource source, Type type); 
        IDataSet GetOrAddDataSet(ICacheSetSource source, Type type); 

        string Prefix { get; }
        IRedisCacher Database { get; }
    } 
}