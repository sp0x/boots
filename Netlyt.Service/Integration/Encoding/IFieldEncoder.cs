using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Netlyt.Service.Source;

namespace Netlyt.Service.Integration.Encoding
{
    public interface IFieldEncoder
    {
        void Apply(BsonDocument doc);
        Task ApplyToAllFields(CancellationToken? cancellationToken = null);
        Task<BulkWriteResult<BsonDocument>> ApplyToField(FieldDefinition field, CancellationToken? cancellationToken = null);
        DataIntegration GetEncodedIntegration(bool truncateDestination = false);
        Task Run(CancellationToken? cancellationToken = null);
    }
}