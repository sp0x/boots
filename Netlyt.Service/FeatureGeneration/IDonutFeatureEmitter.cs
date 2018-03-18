using System.Collections.Generic;
using Netlyt.Service.Donut;
using Netlyt.Service.Integration;

namespace Netlyt.Service.FeatureGeneration
{
    public interface IDonutFeatureEmitter
    {
        IEnumerable<KeyValuePair<string, object>> GetFeatures(IntegratedDocument intDoc);
    }

    public interface IDonutFeatureEmitter<TDonut> : IDonutFeatureEmitter
        where TDonut:IDonutfile
    {
        TDonut DonutFile { get; set; }
    }
}