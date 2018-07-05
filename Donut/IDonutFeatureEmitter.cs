using System.Collections.Generic;
using Netlyt.Interfaces;

namespace Donut
{
    public interface IDonutFeatureEmitter
    {

    }
    public interface IDonutFeatureEmitter<TData>: IDonutFeatureEmitter
        where TData : class, IIntegratedDocument
    {
        IEnumerable<KeyValuePair<string, object>> GetFeatures(TData intDoc);
    }

    public interface IDonutFeatureEmitter<TDonut, TData> : IDonutFeatureEmitter<TData>
        where TDonut: IDonutfile
        where TData : class, IIntegratedDocument
    {
        TDonut DonutFile { get; set; }
    }
}