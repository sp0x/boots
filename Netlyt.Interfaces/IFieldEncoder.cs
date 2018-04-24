using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Netlyt.Interfaces
{
    public interface IFieldEncoder
    {
        void Apply(BsonDocument doc);
        Task ApplyToAllFields(CancellationToken? cancellationToken = null);
        Task<BulkWriteResult<BsonDocument>> ApplyToField(IFieldDefinition field, CancellationToken? cancellationToken = null);
        IIntegration GetEncodedIntegration(bool truncateDestination = false);
        Task Run(CancellationToken? cancellationToken = null);
    }
}