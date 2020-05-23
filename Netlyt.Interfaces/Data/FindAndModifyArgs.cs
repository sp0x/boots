using MongoDB.Bson;
using MongoDB.Driver;

namespace Netlyt.Interfaces.Data
{
    public class FindAndModifyArgs
    {
        public FilterDefinition<BsonDocument> Query { get; set; }
        public UpdateDefinition<BsonDocument> Update { get; set; }
        public FindAndModifyArgs()
        {
        }
    }
    public class FindAndModifyArgs<TRecord>
    {
        public FilterDefinition<TRecord> Query { get; set; }
        public UpdateDefinition<TRecord> Update { get; set; }
        public FindAndModifyArgs()
        {
        }
    }
}
