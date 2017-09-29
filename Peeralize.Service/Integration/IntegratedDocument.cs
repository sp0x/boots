using System;
using System.Threading;
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
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);


        public IntegratedDocument()
        {
            Reserved = new BsonDocument();
        }
        public BsonValue this[string key]
        {
            get
            {
                return Document.Value[key];
            }
            set
            {
                _lock.EnterWriteLock();
                try
                {
                    Document.Value[key] = value;
                }
                finally
                {
                    if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
                }
            }
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
            _lock.EnterWriteLock();
            if (Document != null)
            {
                var bval = itemToAdd.ToBsonDocument();
                var documentValue = Document.Value;

                if (!documentValue.Contains(key))
                {
                    documentValue[key] = new BsonArray();
                }
                ((BsonArray)documentValue[key]).Add(bval);
            }
            if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            return this;
        }

        public string GetString(string key)
        {
            if (Document == null) return null;
            var val = Document.Value;
            return val.Contains(key) ? val[key]?.ToString() : null;
        }
        public long GetInt64(string key)
        {
            return Document!=null ? Document.Value[key].ToInt64() : 0;
        }
        public int? GetInt(string key)
        {
            if (Document == null)
            {
                return null;
            }
            if (!Document.Value.Contains(key))
            {
                return null;
            }; 
            var val = Document.Value[key].ToString();
            if (string.IsNullOrEmpty(val))
            {
                return 0;
            }
            return int.Parse(val);
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

        public DateTime? GetDate(string key)
        {
            var documentValue = Document.Value;
            if (!documentValue.Contains(key))
            {
                return null;
            }
            var s = documentValue[key].ToString();
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }
            return DateTime.Parse(s);
        }
        /// <summary>
        /// Removes all elements by given keys
        /// </summary>
        /// <param name="keys"></param>
        public IntegratedDocument RemoveAll(params string[] keys)
        {
            if (Document != null)
            {
                _lock.EnterWriteLock();
                try
                { 
                    foreach (var key in keys)
                    {
                        Document.Value.Remove(key);
                    }
                }
                finally
                {
                    if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
                }
            }
            return this;
        }

        public IntegratedDocument Define(string key, BsonValue value)
        { 
            this[key] = value;
            return this;
        }

        public bool Has(string key)
        {
            return Document.Value.Contains(key);
        }

        public BsonArray GetArray(string key)
        {
            var doc = GetDocument();
            if (doc == null) return null;
            if (!doc.Contains(key)) return null;
            var value = doc[key].AsBsonArray;
            return value;
        }
    }
     
}