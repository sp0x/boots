using MongoDB.Bson;
using nvoid.db.DB;
using Peeralize.Service.Integration.Blocks;

namespace Peeralize.Service.Integration
{
    public class IntegratedDocument : Entity<int>
    {
        public BsonDocument Document { get; set; }
        public BsonDocument Reserved { get; set; }
        public string UserId { get; set; }
        public string TypeId { get; set; }

        public IntegratedDocument()
        {
            Reserved = new BsonDocument();
        }

        public void SetDocument(dynamic doc)
        {
            Document = ((object)doc).ToBsonDocument(); 
        }

        public IntegratedDocument AddDocumentArrayItem(string key, object itemToAdd)
        {
            var bval = itemToAdd.ToBsonDocument();
            if (!Document.Contains(key))
            {
                Document[key] = new BsonArray();
            }
            ((BsonArray) Document[key]).Add(bval);
            return this;
        }

        public string GetString(string key)
        {
            return Document[key]?.ToString();
        }
        public long GetInt64(string key)
        {
            return Document[key].ToInt64();
        }
        public int GetInt(string key)
        {
            return Document[key].ToInt32();
        }

        public BsonDocument CloneDocument()
        {
            return Document.Clone().ToBsonDocument();
        }

        public IntegratedDocument Clone()
        {
            var newDocument = new IntegratedDocument();
            newDocument.Document = CloneDocument();
            newDocument.Reserved = Reserved.Clone().ToBsonDocument();
            newDocument.UserId = this.UserId;
            newDocument.TypeId = this.TypeId;
            return newDocument;
        }

        public static IntegratedDocument FromType<T>(T visitSession, IntegrationTypeDefinition typedef, string appId)
        { 
            var document = new IntegratedDocument();
            document.Document = visitSession.ToBsonDocument();
            document.TypeId = typedef.Id;
            document.UserId = appId;
            return document;
        }
    }
     
}