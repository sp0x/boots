using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Donut.Features;

namespace Donut.FeatureGeneration
{
    /// <summary>
    /// A tpl block which deals with generating features by multiple functions
    /// </summary>
    public class FeatureGenerator<TIn> : IFeatureGenerator<TIn>
    {
        private List<Func<TIn, IEnumerable<KeyValuePair<string, object>>>> _featureGenerators;
        private int _threadCount;

        /// <summary>
        /// The block that generates features from an inputed document.
        /// </summary>
        //public IPropagatorBlock<IntegratedDocument, DocumentFeatures> Block { get; private set; }
        public FeatureGenerator(int threadCount)
        {
            _featureGenerators = new List<Func<TIn, IEnumerable<KeyValuePair<string, object>>>>();
            _threadCount = threadCount;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="generator">Feature generator based on input documents</param>
        public FeatureGenerator(Func<TIn, IEnumerable<KeyValuePair<string, object>>> generator, int threadCount = 4) 
            : this(threadCount)
        {
            if(generator!=null) _featureGenerators.Add(generator);
        }

        public FeatureGenerator(IEnumerable<Func<TIn, IEnumerable<KeyValuePair<string, object>>>> generators,
            int threadCount = 4) : this(threadCount)
        {
            if (generators != null)
            {
                _featureGenerators.AddRange(generators);
            }
        }

        public IFeatureGenerator<TIn> AddGenerator(
            Func<TIn, IEnumerable<KeyValuePair<string, object>>> generator)
        {
            _featureGenerators.Add(generator);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IPropagatorBlock<TIn, FeaturesWrapper<TIn>> CreateFeaturesBlock()
        {
            return CreateFeaturesBlock<FeaturesWrapper<TIn>>();
        }

        /// <summary>
        /// Creates a new transformer block that creates features 
        /// </summary>
        /// <typeparam name="T">A features wrapper</typeparam>
        /// <returns></returns>
        public IPropagatorBlock<TIn, T> CreateFeaturesBlock<T>()
            where T :  FeaturesWrapper<TIn>, new()
        {
            var options = new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = _threadCount};
            var queueLock = new object(); 
            var transformerBlock = new TransformBlock<TIn, T>((doc) =>
            {
                var queue = new Queue<KeyValuePair<string, object>>();
                //Go through each function that generates features
                Parallel.ForEach(_featureGenerators, (ftrGen) =>
                {
                    //Feed the function with the document
                    var features = ftrGen(doc);
                    foreach (var feature in features)
                    {
                        lock (queueLock)
                        {
                            //Append each feature
                            queue.Enqueue(feature);
                        }
                    }
                }); 
                T featuresDoc = new T();
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
            var transformers = _featureGenerators
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
