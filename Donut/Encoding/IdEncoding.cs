using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Donut.Integration;
using Donut.Source;
using MongoDB.Bson;
using MongoDB.Driver;
using Netlyt.Interfaces;

namespace Donut.Encoding
{
    public class IdEncoding : FieldEncoding
    {
        
        public IdEncoding(FieldEncodingOptions options) : base(options, FieldDataEncoding.Id)
        {

        }

        public override void Apply(BsonDocument doc)
        {
            foreach (var field in TargetFields)
            {
                var docFieldVal = doc[field.Name].ToString();
                var categories = FieldCache[field.Name];
                IFieldExtra extrasCategory = null;
                extrasCategory = categories.GetOrAdd(docFieldVal, (key) =>
                {
                    var newExtra = new FieldExtra()
                    {
                        Key = (categories.Count + 1).ToString(),
                        Value = docFieldVal
                    };
                    if (field.Extras == null)
                    {
                        field.Extras = new FieldExtras();
                    }
                    field.Extras.Extra.Add(newExtra);
                    return newExtra;
                });
                doc[field.Name] = extrasCategory.Key;
            }
        }

        public override async Task<BulkWriteResult<BsonDocument>> ApplyToField(IFieldDefinition field, 
            IMongoCollection<BsonDocument> collection, 
            CancellationToken? cancellationToken = null)
        {
            if (cancellationToken == null) cancellationToken = CancellationToken.None;
            var updateModels = new WriteModel<BsonDocument>[field.Extras.Extra.Count];
            int iModel = 0;
            var dummies = field.Extras.Extra;
            foreach (var column in dummies)
            {
                var query = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq(field.Name, column.Value)
                );
                var updates = new List<UpdateDefinition<BsonDocument>>();
                updates.Add(Builders<BsonDocument>.Update.Set(field.Name, column.Key));
                var qrUpdateRoot = Builders<BsonDocument>.Update.Combine(updates);
                var actionModel = new UpdateManyModel<BsonDocument>(query, qrUpdateRoot);
                updateModels[iModel++] = actionModel;
            }
            var result = await collection.BulkWriteAsync(updateModels, new BulkWriteOptions()
            {
            }, cancellationToken.Value);
            return result;
        }


        public override IIntegration GetEncodedIntegration(bool truncateDestination = false)
        {
            throw new System.NotImplementedException();
        }

        public override IEnumerable<KeyValuePair<string, int>> GetDecodedFieldpairs(FieldDefinition field, BsonDocument document)
        {
            yield return new KeyValuePair<string, int>(field.Name, document[field.Name].AsInt32);
        }

        public override IEnumerable<string> GetEncodedFieldNames(IFieldDefinition fld)
        {
            yield return fld.Name;
        }

        public override string DecodeField(FieldDefinition field, BsonValue value)
        {
            throw new NotImplementedException();
        }
    }
}