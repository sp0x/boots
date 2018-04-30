using System.Threading;
using System.Threading.Tasks;
using Donut.Integration;
using Donut.Source;
using MongoDB.Bson;
using MongoDB.Driver;
using Netlyt.Interfaces;

namespace Donut.Encoding
{
    public interface IFieldEncoder
    {
        void Apply(BsonDocument doc);
        Task ApplyToAllFields(IMongoCollection<BsonDocument> collection, CancellationToken? cancellationToken = null);
        Task<BulkWriteResult<BsonDocument>> ApplyToField(IFieldDefinition field, IMongoCollection<BsonDocument> collection, CancellationToken? cancellationToken = null);
        IIntegration GetEncodedIntegration(bool truncateDestination = false);
        Task Run(IMongoCollection<BsonDocument> collection, CancellationToken? cancellationToken = null);
    }
}