using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks.Dataflow;
using MongoDB.Bson;
using nvoid.Helpers;

namespace Peeralize.Service.Integration.Blocks
{
    public abstract class BsonConverter 
    {
        public static TransformBlock<ExpandoObject[], IEnumerable<BsonDocument>> Create(ExecutionDataflowBlockOptions options = null)
        {
            if(options==null) options = new ExecutionDataflowBlockOptions {BoundedCapacity = 1};
            return new TransformBlock<ExpandoObject[], IEnumerable<BsonDocument>>(values =>
                values.Select(ExpandoWrapper.ToBson), options);
        }
    }
}
