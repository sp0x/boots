using Donut.Integration;
using MongoDB.Bson;
using MongoDB.Driver;
using Netlyt.Interfaces;

namespace Donut.Data
{
    public class DataImportResult
    {
        public HarvesterResult Data { get; private set; }
        public IMongoCollection<BsonDocument> Collection { get; private set; }
        public IIntegration Integration { get; private set; }
        public DataImportResult(HarvesterResult data, IMongoCollection<BsonDocument> collection, IIntegration integration)
        {
            this.Collection = collection;
            this.Data = data;
            this.Integration = integration;
        }
    }
}