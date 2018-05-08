using System.Collections.Generic;

namespace Netlyt.Interfaces
{
    public interface IDataSet
    {
        void SetSource(string collection);
        void SetAggregateKeys(IEnumerable<IAggregateKey> keys);
    }

    public interface IDataSet<T> : IDataSet
        where T : class
    { 
    }
}