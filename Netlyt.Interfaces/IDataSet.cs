using System.Collections.Generic;
using Donut;

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