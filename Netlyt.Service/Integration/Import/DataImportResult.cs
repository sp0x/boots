using nvoid.db.DB.MongoDB;

namespace Netlyt.Service.Integration.Import
{
    public class DataImportResult
    {
        public HarvesterResult Data { get; private set; }
        public MongoList Collection { get; private set; }
        public DataImportResult(HarvesterResult data, MongoList collection)
        {
            this.Collection = collection;
            this.Data = data;
        }
    }
}