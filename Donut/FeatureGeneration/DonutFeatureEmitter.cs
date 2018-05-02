using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using Netlyt.Interfaces;

namespace Donut.FeatureGeneration
{
    /// <summary>
    /// Base class for feature emitters that work on documents.
    /// </summary>
    /// <typeparam name="TDonut"></typeparam>
    /// <typeparam name="TContext"></typeparam>
    public abstract class DonutFeatureEmitter<TDonut, TContext, TData> : IDonutFeatureEmitter<TDonut, TData>
        where TDonut : Donutfile<TContext, TData> 
        where TContext : DonutContext
        where TData : class, IIntegratedDocument
    {  
        public TDonut DonutFile { get; set; }
        public DonutFeatureEmitter(TDonut donut)
        {
            this.DonutFile = donut;
        }

        /// <summary>
        /// Gets a transform block that transforms documents to feature pairs.
        /// </summary>
        /// <returns></returns>
        public TransformBlock<TData, IEnumerable<KeyValuePair<string, object>>> GetBlock()
        {
            var block = new TransformBlock<TData, IEnumerable<KeyValuePair<string, object>>>((doc) =>
            {
                return GetFeatures(doc);
            });
            return block;
        }
        /// <summary>
        /// Gets the feature pairs from a document
        /// </summary>
        /// <param name="intDoc"></param>
        /// <returns></returns>
        public abstract IEnumerable<KeyValuePair<string, object>> GetFeatures(TData intDoc);
    }
}