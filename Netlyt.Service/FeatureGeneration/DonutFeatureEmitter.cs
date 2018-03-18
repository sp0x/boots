using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
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

        public TransformBlock<IntegratedDocument, IEnumerable<KeyValuePair<string, object>>> GetBlock()
        {
            var block = new TransformBlock<IntegratedDocument, IEnumerable<KeyValuePair<string, object>>>((doc) =>
            {
                return GetFeatures(doc);
            });
            return block;
        }
        public abstract IEnumerable<KeyValuePair<string, object>> GetFeatures(IntegratedDocument intDoc);
    }
}