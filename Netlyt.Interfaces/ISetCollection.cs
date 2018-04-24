using System;

namespace Netlyt.Interfaces
{
    public interface ISetCollection
    {
        ICacheSet GetOrAddSet(ICacheSetSource source, Type type); 
        IDataSet GetOrAddDataSet(ICacheSetSource source, Type type); 

        string Prefix { get; }
        IRedisCacher Database { get; }
    } 
}