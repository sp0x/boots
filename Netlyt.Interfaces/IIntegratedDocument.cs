using System;
using MongoDB.Bson;

namespace Netlyt.Interfaces
{
    public interface IIntegratedDocument
    {
        BsonValue this[string key] { get; set; }

        long APIId { get; set; }
        Lazy<BsonDocument> Document { get; set; }
        string Id { get; set; }
        long IntegrationId { get; set; }
        BsonDocument Reserved { get; set; }

        IIntegratedDocument AddDocumentArrayItem(string key, object itemToAdd);
        IIntegratedDocument Clone();
        BsonDocument CloneDocument();
        IIntegratedDocument Define(string key, BsonValue value);
        BsonArray GetArray(string key);
        DateTime? GetDate(string key);
        BsonDocument GetDocument();
        int? GetInt(string key);
        long GetInt64(string key);
        string GetString(string key);
        bool Has(string key);
        IIntegratedDocument RemoveAll(params string[] keys);
        void SetDocument(dynamic doc);
    }
}