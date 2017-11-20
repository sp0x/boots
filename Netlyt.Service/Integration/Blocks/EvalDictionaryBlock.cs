using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MongoDB.Bson;

namespace Netlyt.Service.Integration.Blocks
{
    public class EvalDictionaryBlock
        : BaseFlowBlock<IntegratedDocument, IntegratedDocument>
    {
        private Action<IntegratedDocument, BsonDocument> _action;
        private Func<IntegratedDocument, BsonArray> _childSelector;
        private Func<IntegratedDocument, object> _keyResolver;

        public ConcurrentDictionary<object, IntegratedDocument> Elements { get; set; }
        
        public CrossSiteAnalyticsHelper Helper { get; set; }

        public EvalDictionaryBlock(
            Func<IntegratedDocument, object> keyResolver,
            Action<IntegratedDocument, BsonDocument> action,
            Func<IntegratedDocument, BsonArray> childSelector,
            int threadCount = 4) : base(capacity: 1000 * 30, procType: ProcessingType.Action, threadCount: threadCount)
        {
            _keyResolver = keyResolver;
            _action = action;
            _childSelector = childSelector;
            Elements = new ConcurrentDictionary<object, IntegratedDocument>(); 
        }

        protected override IntegratedDocument OnBlockReceived(IntegratedDocument intDoc)
        {
            var doc = intDoc.GetDocument();
            var key = _keyResolver(intDoc);
            if (Elements.ContainsKey(key))
            {
                throw new Exception("EvalDictionaryBlock supports only 1 item to be added with the same key!");
            }
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