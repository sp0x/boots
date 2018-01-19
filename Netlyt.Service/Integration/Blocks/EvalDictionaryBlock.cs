using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MongoDB.Bson;
using nvoid.exec.Blocks;

namespace Netlyt.Service.Integration.Blocks
{
    /// <summary>
    /// 
    /// </summary>
    public class EvalDictionaryBlock
        : BaseFlowBlock<IntegratedDocument, IntegratedDocument>
    {
        private Action<IntegratedDocument, BsonDocument> _action;
        /// <summary>
        /// 
        /// </summary>
        private Func<IntegratedDocument, BsonArray> _childSelector;
        /// <summary>
        /// Function used to resolve the key from an integrated document.
        /// Keys should be unique.
        /// </summary>
        private Func<IntegratedDocument, object> _keyResolver;
        /// <summary>
        /// 
        /// </summary>
        public ConcurrentDictionary<object, IntegratedDocument> Elements { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public CrossSiteAnalyticsHelper Helper { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyResolver">Key generation method. Only unique keys are supported.</param>
        /// <param name="action">The action to perform on each child fetched from childSelector</param>
        /// <param name="childSelector">Children fetcher.</param>
        /// <param name="threadCount"></param>
        public EvalDictionaryBlock(
            Func<IntegratedDocument, object> keyResolver,
            Action<IntegratedDocument, BsonDocument> action,
            Func<IntegratedDocument, BsonArray> childSelector,
            int threadCount = 4,
            int capacity = 1000) : base(capacity: capacity, procType: BlockType.Action, threadCount: threadCount)
        {
            _keyResolver = keyResolver;
            _action = action;
            _childSelector = childSelector;
            Elements = new ConcurrentDictionary<object, IntegratedDocument>(); 
        }

        /// <summary>
        /// Handle the blocks
        /// </summary>
        /// <param name="intDoc"></param>
        /// <returns></returns>
        protected override IntegratedDocument OnBlockReceived(IntegratedDocument intDoc)
        { 
            var key = _keyResolver(intDoc);
//            if (Elements.ContainsKey(key))
//            {
//                throw new Exception("EvalDictionaryBlock supports only 1 item to be added with the same key!");
//            }
//Save the element
            if (!Elements.TryAdd(key, intDoc))
            {
                throw new Exception("EvalDictionaryBlock supports only 1 item to be added with the same key!");
            }
            BsonArray children = _childSelector(intDoc); 
            if (children != null)
            {
                foreach (BsonDocument child in children)
                {
                    _action(intDoc, child);
                }
            }
            return intDoc;
        }

        public override void Complete()
        {
            base.Complete();
        }

        protected override IEnumerable<IntegratedDocument> GetCollectedItems()
        {
            return Elements.Values;
        }
    }
}