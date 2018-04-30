using Donut;
using Donut.Integration;
using nvoid.db.DB.MongoDB;
using Netlyt.Interfaces;

namespace Netlyt.Service.Integration.Import
{
    public class DataImportResult
    {
        public HarvesterResult Data { get; private set; }
        public MongoList Collection { get; private set; }
        public IIntegration Integration { get; private set; }
        public DataImportResult(HarvesterResult data, MongoList collection, IIntegration integration)
        {
            this.Collection = collection;
            this.Data = data;
            this.Integration = integration;
        }
    }
}