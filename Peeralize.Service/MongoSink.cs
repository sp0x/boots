using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using nvoid.db.DB.RDS;
using nvoid.db.Extensions;
using Peeralize.Service.DataSets;
using Peeralize.Service.Integration;
using Peeralize.Service.Source;

namespace Peeralize.Service
{
    public class MongoSink : IIntegrationDestination
    { 
        private BufferBlock<IntegratedDocument> _buffer;
        private TransformBlock<IntegratedDocument, IntegratedDocument> _mongoBlock; 
        private RemoteDataSource<IntegratedDocument> _source;
        
        public string UserId { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="capacity">The capacity of this sink. If more blocks are posted, it will block untill there is some free space.</param>
        public MongoSink(string userId, int capacity = 1000 * 1000)
        {
            UserId = userId;
            _source = typeof(IntegratedDocument).GetDataSource<IntegratedDocument>();
            var options = new DataflowBlockOptions()
            {
                BoundedCapacity = capacity,
                EnsureOrdered = true
            };
            _buffer = new BufferBlock<IntegratedDocument>(options);
            _mongoBlock = new TransformBlock<IntegratedDocument, IntegratedDocument>(new Func<IntegratedDocument, IntegratedDocument>(OnBlockReceived));
            _buffer.LinkTo(_mongoBlock);
        }

        /// <summary>
        /// Consumes available integration documents.
        /// </summary>
        public async void ConsumeAsync(CancellationToken token)
        {
            if (UserId == null)
                throw new InvalidOperationException("User id must be set in order to push data to this destination.");
            while (await _buffer.OutputAvailableAsync(token))
            {
//                IntegratedDocument newDocument = _buffer.Receive();
//                newDocument.UserId = UserId;
////                if (newDocument.Document==null | newDocument.Document.ElementCount <= 1)
////                {
////                    newDocument = newDocument;
////                }
//                _source.Save(newDocument);
                Console.WriteLine($@"{DateTime.Now}: Current load: {_buffer.Count} items");
            }
        }

        /// <summary>
        /// Consumes available integration documents.
        /// </summary>
        public void Consume()
        {
            if (UserId == null)
                throw new InvalidOperationException("User id must be set in order to push data to this destination.");
            while (_buffer.OutputAvailableAsync().Result)
            {
                Console.WriteLine($@"{DateTime.Now}: Current load: {_buffer.Count} items");
            }
        }

        private IntegratedDocument OnBlockReceived(IntegratedDocument doc)
        {
            doc.UserId = UserId;
            //                if (newDocument.Document==null | newDocument.Document.ElementCount <= 1)
            //                {
            //                    newDocument = newDocument;
            //                }
            _source.Save(doc);
            return doc;
        }


        public void Close()
        {
            _buffer.Complete(); 
        }
        /// <summary>
        /// Adds the item to the sink
        /// </summary>
        /// <param name="item"></param>
        public void Post(IntegratedDocument item)
        {
            _buffer.Post(item);
        }

        /// <summary>
        /// Adds the item to the sink
        /// </summary>
        /// <param name="item"></param>
        public async Task<bool> PostAsync(IntegratedDocument item)
        {
            return await _buffer.SendAsync(item);
        }

        public IIntegrationDestination LinkTo(ITargetBlock<IntegratedDocument> target, DataflowLinkOptions linkOptions = null)
        {
            if (linkOptions != null)
            {
                _mongoBlock.LinkTo(target, linkOptions);
            }
            else
            {
                _mongoBlock.LinkTo(target);
            }
            
            return this;
        }
    }
}
