using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private RemoteDataSource<IntegratedDocument> _source;
        
        public string UserId { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="capacity"></param>
        public MongoSink(string userId, int capacity = 1000 * 1000)
        {
            UserId = userId;
            _source = typeof(IntegratedDocument).GetDataSource<IntegratedDocument>();
            var options = new DataflowBlockOptions()
            {
                BoundedCapacity = capacity
            };
            _buffer = new BufferBlock<IntegratedDocument>(options);
        }

        /// <summary>
        /// Consumes available integration documents.
        /// </summary>
        public async void Consume()
        {
            if (UserId == null)
                throw new InvalidOperationException("User id must be set in order to push data to this destination.");
            while (await _buffer.OutputAvailableAsync())
            {
                IntegratedDocument newDocument = _buffer.Receive();
                newDocument.UserId = UserId;
                if (newDocument.Document==null | newDocument.Document.ElementCount <= 1)
                {
                    newDocument = newDocument;
                }
                _source.Save(newDocument);
                Console.WriteLine($@"{DateTime.Now}: Current load: {_buffer.Count} items");
            }
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
    }
}
