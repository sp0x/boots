using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using MongoDB.Bson;

namespace Netlyt.Interfaces.Batching
{
    public abstract class BsonConverter 
    {
        /// <summary>
        /// Creates a new TransformBlock that converts arrays of dynamic objects to bson objects.
        /// </summary>
        ///
        /// <remarks>   Vasko, 07-Dec-17. </remarks>
        ///
        /// <param name="options">  (Optional) Options for controlling the operation. </param>
        ///
        /// <returns>   A TransformBlock&lt;ExpandoObject[],IEnumerable&lt;BsonDocument&gt;&gt; </returns>

        public static TransformBlock<ExpandoObject[], IEnumerable<BsonDocument>> CreateManyBlock(ExecutionDataflowBlockOptions options = null)
        {
            if(options==null) options = new ExecutionDataflowBlockOptions {BoundedCapacity = 1};
            Func<ExpandoObject, BsonDocument> mapper = x =>
            {
                var doc = new BsonDocument();
                foreach (var pair in x)
                {
                    doc.Set(pair.Key, BsonValue.Create(pair.Value));
                }
                return doc;
            };
            return new TransformBlock<ExpandoObject[], IEnumerable<BsonDocument>>(values =>
                values.Select(mapper), options);

        }

        /// <summary>
        /// Creates a new TransformBlock that converts a dynamic object to bson object.
        /// </summary>
        ///
        /// <remarks>   Vasko, 07-Dec-17. </remarks>
        ///
        /// <param name="options">  (Optional) Options for controlling the operation. </param>
        ///
        /// <returns>   A TransformBlock&lt;ExpandoObject[],IEnumerable&lt;BsonDocument&gt;&gt; </returns>

        public static TransformBlock<ExpandoObject, BsonDocument> CreateBlock(ExecutionDataflowBlockOptions options = null)
        {
            if (options == null) options = new ExecutionDataflowBlockOptions { BoundedCapacity = 1 };
            return new TransformBlock<ExpandoObject, BsonDocument>(v =>
                v.ToBsonDocument(), options);
        }
    }
}
