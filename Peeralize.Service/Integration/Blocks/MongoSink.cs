using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.RDS;
using nvoid.db.Extensions;
using Peeralize.Service.DataSets;
using Peeralize.Service.Integration;
using Peeralize.Service.Source;

namespace Peeralize.Service.Integration.Blocks
{
    public class MongoSink 
        : BaseFlowBlock<IntegratedDocument, IntegratedDocument>
    {  
        private RemoteDataSource<IntegratedDocument> _source;
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="capacity">The capacity of this sink. If more blocks are posted, it will block untill there is some free space.</param>
        public MongoSink(string userId, int capacity = 1000 * 1000, int threadCount = 10) 
            : base(capacity, ProcessingType.Action, threadCount: threadCount)
        {
            UserId = userId;
            _source = typeof(IntegratedDocument).GetDataSource<IntegratedDocument>(); 
            
        }


        protected override IEnumerable<IntegratedDocument> GetCollectedItems()
        {
            return null;
        }

        protected override IntegratedDocument OnBlockReceived(IntegratedDocument intDoc)
        {
            intDoc.UserId = UserId;
            //                if (newDocument.Document==null | newDocument.Document.ElementCount <= 1)
            //                {
            //                    newDocument = newDocument;
            //                }
            _source.Save(intDoc);
            return intDoc;
        }


        
    }
}
