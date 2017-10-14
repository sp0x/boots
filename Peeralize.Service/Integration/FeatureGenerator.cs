using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Peeralize.Service.Models;

namespace Peeralize.Service.Integration
{
    /// <summary>
    /// A tpl block which
    /// </summary>
    public class FeatureGenerator<TIn>
    {
        private List<Func<TIn, IEnumerable<KeyValuePair<string, object>>>> _generators;
        private int _threadCount;

        /// <summary>
        /// The block that generates features from an inputed document.
        /// </summary>
        //public IPropagatorBlock<IntegratedDocument, DocumentFeatures> Block { get; private set; }
        public FeatureGenerator(int threadCount)
        {
            _generators = new List<Func<TIn, IEnumerable<KeyValuePair<string, object>>>>();
            _threadCount = threadCount;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="generator">Feature generator based on input documents</param>
        public FeatureGenerator(Func<TIn, IEnumerable<KeyValuePair<string, object>>> generator, int threadCount = 4) 
            : this(threadCount)
        {
            if(generator!=null) _generators.Add(generator);
        }

        public FeatureGenerator(IEnumerable<Func<TIn, IEnumerable<KeyValuePair<string, object>>>> generators,
            int threadCount = 4) : this(threadCount)
        {
            if (generators != null)
            {
                _generators.AddRange(generators);
            }
        }

        public FeatureGenerator<TIn>
            AddGenerator(
            Func<TIn, IEnumerable<KeyValuePair<string, object>>> generator)
        {
            _generators.Add(generator);
            return this;
        }

        public IPropagatorBlock<TIn, FeaturesWrapper<TIn>> CreateFeaturesBlock()
        {
            return CreateFeaturesBlock<FeaturesWrapper<TIn>>();
        }
        public IPropagatorBlock<TIn, T> CreateFeaturesBlock<T>()
            where T : FeaturesWrapper<TIn>
        {
            var options = new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = _threadCount};
            var queueLock = new object();
            var transformerBlock = new TransformBlock<TIn, T>((doc) =>
            {
                var queue = new Queue<KeyValuePair<string, object>>();
                Parallel.ForEach(_generators, (generator) =>
                {
                    var features = generator(doc);
                    foreach (var feature in features)
                    {
                        lock (queueLock)
                        {
                            queue.Enqueue(feature);
                        }
                    }
                });
                var type = typeof(T);
                var constructorInfo = type.GetConstructor(new Type[] { });
                T featuresDoc = constructorInfo.Invoke(new object[]{}) as T;
                Debug.Assert(featuresDoc != null, nameof(featuresDoc) + " != null");
                featuresDoc.Document = doc;
                featuresDoc.Features = queue; 
                return featuresDoc;
            }, options);
            return transformerBlock;
        }

        /// <summary>
        /// Create a feature generator block, with all the current feature generators.
        /// </summary>
        /// <param name="threadCount"></param>
        /// <returns></returns>
        public IPropagatorBlock<TIn, IEnumerable<KeyValuePair<string, object>>> CreateFeaturePairsBlock()
        {
            //Dataflow: poster -> each transformer -> buffer
            var buffer = new BufferBlock<IEnumerable<KeyValuePair<string, object>>>();
            // The target part receives data and adds them to the queue.
            var transformers = _generators
                .Select(x =>
                {
                    var transformer =
                        new TransformBlock<TIn, IEnumerable<KeyValuePair<string, object>>>(x);
                    transformer.LinkTo(buffer);
                    return transformer;
                });
            var postOptions = new ExecutionDataflowBlockOptions();
            postOptions.MaxDegreeOfParallelism = _threadCount;
            //Post an item to each transformer
            var poster = new ActionBlock<TIn>(doc =>
            {
                foreach (var transformer in transformers)
                {
                    transformer.Post(doc);
                }
            }, postOptions);
            // Return a IPropagatorBlock<T, T[]> object that encapsulates the 
            // target and source blocks.
            return DataflowBlock.Encapsulate(poster, buffer);
        }
    }
}
