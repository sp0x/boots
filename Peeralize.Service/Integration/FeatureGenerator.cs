using System;
using System.Collections.Generic;
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
    public class FeatureGenerator
    {
        private List<Func<IntegratedDocument, IEnumerable<KeyValuePair<string, object>>>> _generators;
        private int _threadCount;

        /// <summary>
        /// The block that generates features from an inputed document.
        /// </summary>
        //public IPropagatorBlock<IntegratedDocument, DocumentFeatures> Block { get; private set; }
        public FeatureGenerator(int threadCount)
        {
            _generators = new List<Func<IntegratedDocument, IEnumerable<KeyValuePair<string, object>>>>();
            _threadCount = threadCount;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="generator">Feature generator based on input documents</param>
        public FeatureGenerator(Func<IntegratedDocument, IEnumerable<KeyValuePair<string, object>>> generator, int threadCount = 4) 
            : this(threadCount)
        {
            if(generator!=null) _generators.Add(generator);
        }

        public FeatureGenerator(IEnumerable<Func<IntegratedDocument, IEnumerable<KeyValuePair<string, object>>>> generators,
            int threadCount = 4) : this(threadCount)
        {
            if (generators != null)
            {
                _generators.AddRange(generators);
            }
        }

        public FeatureGenerator AddGenerator(
            Func<IntegratedDocument, IEnumerable<KeyValuePair<string, object>>> generator)
        {
            _generators.Add(generator);
            return this;
        }

        public IPropagatorBlock<IntegratedDocument, DocumentFeatures> CreateFeaturesBlock()
        {
            var options = new ExecutionDataflowBlockOptions();
            options.MaxDegreeOfParallelism = _threadCount;
            var queueLock = new object();
            var transformerBlock = new TransformBlock<IntegratedDocument, DocumentFeatures>((doc) =>
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
                var featuresDoc = new DocumentFeatures(doc, queue);
                return featuresDoc;
            }, options);
            return transformerBlock;
        }

        /// <summary>
        /// Create a feature generator block, with all the current feature generators.
        /// </summary>
        /// <param name="threadCount"></param>
        /// <returns></returns>
        public IPropagatorBlock<IntegratedDocument, IEnumerable<KeyValuePair<string, object>>> CreateFeaturePairsBlock()
        {
            //Dataflow: poster -> each transformer -> buffer
            var buffer = new BufferBlock<IEnumerable<KeyValuePair<string, object>>>();
            // The target part receives data and adds them to the queue.
            var transformers = _generators
                .Select(x =>
                {
                    var transformer =
                        new TransformBlock<IntegratedDocument, IEnumerable<KeyValuePair<string, object>>>(x);
                    transformer.LinkTo(buffer);
                    return transformer;
                });
            var postOptions = new ExecutionDataflowBlockOptions();
            postOptions.MaxDegreeOfParallelism = _threadCount;
            //Post an item to each transformer
            var poster = new ActionBlock<IntegratedDocument>(doc =>
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
