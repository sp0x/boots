using System.Collections.Generic;

namespace Netlyt.Interfaces
{
    public interface IFeaturesWrapper
    {
        IEnumerable<KeyValuePair<string, object>> Features { get; set; }
    }
    public interface IFeaturesWrapper<T> : IFeaturesWrapper
    {
        T Document { get; set; }
    }
}