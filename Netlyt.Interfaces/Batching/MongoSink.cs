using System.Collections.Generic;
using MongoDB.Driver;
using Netlyt.Interfaces.Blocks;
using Netlyt.Interfaces.Data;

namespace Netlyt.Interfaces.Batching
{
    public class MongoSink<T>
        : BaseFlowBlock<T, T>
        where T : class
    {  
        private IMongoCollection<T> _source;
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="capacity">The capacity of this sink. If more blocks are posted, it will block untill there is some free space.</param>
        public MongoSink(string appId, int capacity = 1000 * 1000, int threadCount = 10) 
            : base(capacity, BlockType.Action, threadCount: threadCount)
        {
            AppId = appId;
            _source = MongoHelper.GetTypeCollection<T>();
        }


        protected override IEnumerable<T> GetCollectedItems()
        {
            return null;
        }

        protected override T OnBlockReceived(T intDoc)
        {
            //intDoc.UserId = UserId;
            //                if (newDocument.Document==null | newDocument.Document.ElementCount <= 1)
            //                {
            //                    newDocument = newDocument;
            //                }
            _source.InsertOne(intDoc);
            return intDoc;
        }


        
    }
}
