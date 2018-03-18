using System.Collections.Generic;
using Netlyt.Service.Donut;
using Netlyt.Service.FeatureGeneration;

namespace Netlyt.Service.Integration
{
    public abstract class DonutFeatureEmitter<TDonut, TContext> : IDonutFeatureEmitter<TDonut>
        where TDonut : Donutfile<TContext> 
        where TContext : DonutContext
    {  
        public TDonut DonutFile { get; set; }
        public DonutFeatureEmitter(TDonut donut)
        {
            this.DonutFile = donut;
        }

        public abstract IEnumerable<KeyValuePair<string, object>> GetFeatures(IntegratedDocument intDoc);
    }
}