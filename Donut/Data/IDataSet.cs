using System.Collections.Generic;

namespace Donut.Data
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