using System;
using MongoDB.Bson;
using nvoid.db.DB;
using Peeralize.Service.Integration.Blocks;

namespace Peeralize.Service.Integration
{
    public class IntegratedDocument : Entity<int>
    {
        public Lazy<BsonDocument> Document { get; set; }
        public BsonDocument Reserved { get; set; }
        public string UserId { get; set; }
        public string TypeId { get; set; }

        public IntegratedDocument()
        {
            Reserved = new BsonDocument();
        }

        public void SetDocument(dynamic doc)
        { 
            Document = new Lazy<BsonDocument>(() => ((object)doc).ToBsonDocument()); ; 
        }

        public BsonDocument GetDocument()
        {
            return Document?.Value;
        }
        public IntegratedDocument AddDocumentArrayItem(string key, object itemToAdd)
        {
            if (Document != null)
            {
                var bval = itemToAdd.ToBsonDocument();
                if (!Document.Value.Contains(key))
                {
                    Document.Value[key] = new BsonArray();
                }
                ((BsonArray)Document.Value[key]).Add(bval);
            }
            return this;
        }

        public string GetString(string key)
        {
            return Document!=null ? Document.Value[key]?.ToString() : null;
        }
        public long GetInt64(string key)
        {
            return Document!=null ? Document.Value[key].ToInt64() : 0;
        }
        public int GetInt(string key)
        {
            return Document!=null ? Document.Value[key].ToInt32() : 0;
        }

        public BsonDocument CloneDocument()
        {
            return Document!=null ? Document.Value.Clone().ToBsonDocument() : null;
        }

        public IntegratedDocument Clone()
        {
            var newDocument = new IntegratedDocument();
            newDocument.Document = new Lazy<BsonDocument>(CloneDocument);
            newDocument.Reserved = Reserved.Clone().ToBsonDocument();
            newDocument.UserId = this.UserId;
            newDocument.TypeId = this.TypeId;
            return newDocument;
        }

        public static IntegratedDocument FromType<T>(T visitSession, IntegrationTypeDefinition typedef, string appId)
        { 
            var document = new IntegratedDocument();
            document.Document = new Lazy<BsonDocument>(()=> visitSession.ToBsonDocument());
            document.TypeId = typedef.Id.Value;
            document.UserId = appId;
            return document;
        }
    }
     
}