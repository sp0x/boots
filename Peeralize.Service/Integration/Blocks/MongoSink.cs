using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks.Dataflow;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using nvoid.db.DB.RDS;
using nvoid.db.Extensions;
using Peeralize.Service.DataSets;
using Peeralize.Service.Integration;
using Peeralize.Service.Source;

namespace Peeralize.Service.Integration.Blocks
{
    public class MongoSink : IntegrationBlock
    {  
        private RemoteDataSource<IntegratedDocument> _source;
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="capacity">The capacity of this sink. If more blocks are posted, it will block untill there is some free space.</param>
        public MongoSink(string userId, int capacity = 1000 * 1000) : base(capacity)
        {
            UserId = userId;
            _source = typeof(IntegratedDocument).GetDataSource<IntegratedDocument>(); 
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
