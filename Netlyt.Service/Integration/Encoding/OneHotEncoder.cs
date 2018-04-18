using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using Netlyt.Service.Integration.Encoding;
using Netlyt.Service.Source;

namespace Netlyt.Service.Integration.Encoding
{
    public class OneHotEncoder : FieldEncoder
    {
        public OneHotEncoder(FieldEncodingOptions options) : base(options, FieldDataEncoding.OneHot)
        {
        }
        
        public override async Task<BulkWriteResult<BsonDocument>> ApplyToField(FieldDefinition field, CancellationToken? cancellationToken = null)
        {
            if (cancellationToken == null) cancellationToken = CancellationToken.None;
            var updateModels = new WriteModel<BsonDocument>[field.Extras.Extra.Count];
            int iModel = 0;
            var dummies = field.Extras.Extra;
            foreach (var column in dummies)
            {
                var otherDummies = dummies.Where(x => x.Key != column.Key);
                var query = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq(field.Name, column.Value)
                );
                var updates = new List<UpdateDefinition<BsonDocument>>();
                updates.Add(Builders<BsonDocument>.Update.Set(column.Key, 1));
                foreach (var dummy in otherDummies) updates.Add(Builders<BsonDocument>.Update.Set(dummy.Key, 0));
                var qrUpdateRoot = Builders<BsonDocument>.Update.Combine(updates);
                var actionModel = new UpdateManyModel<BsonDocument>(query, qrUpdateRoot);
                updateModels[iModel++] = actionModel;
            }
            var result = await Records.BulkWriteAsync(updateModels, new BulkWriteOptions()
            {
            }, cancellationToken.Value);
            return result;
        }

        public override DataIntegration GetEncodedIntegration(bool truncateDestination = false)
        {
            var databaseConfiguration = DBConfig.GetGeneralDatabase();
            var collection = Integration.Collection;
            var dstCollection = new MongoList(databaseConfiguration, collection);

            if (truncateDestination)
            {
                dstCollection.Truncate();
            }
            var records = dstCollection.Records;
            foreach (var oneHotField in TargetFields)
            {
                var fld = oneHotField;
                //Run a group aggregate
                var pipeline = new List<BsonDocument>();
                var group = new BsonDocument();
                group["$group"] = new BsonDocument() { { "_id", $"${fld.Name}" } };
                pipeline.Add(group);
                var uniqueColumnResults = records.Aggregate<BsonDocument>(pipeline).ToList();
                int iVariation = fld.Extras.Extra==null ? 1 : fld.Extras.Extra.Count+1;
                foreach (var uniqueValue in uniqueColumnResults)
                {
                    var columnVal = uniqueValue["_id"].ToString();
                    if (fld.Extras.Extra.Any(y => y.Value == columnVal)) continue;
                    var fieldExtra = new FieldExtra()
                    {
                        Field = fld,
                        Key = fld.Name + iVariation++,
                        Value = columnVal,
                        Type = FieldExtraType.Dummy
                    };
                    if (fld.Extras == null) fld.Extras = new FieldExtras();
                    fld.Extras.Extra.Add(fieldExtra);
                }
            }
            return Integration;
        }

        public override void Apply(BsonDocument doc)
        {
            foreach (var field in TargetFields)
            {
                var docFieldVal = doc[field.Name].ToString();
                var categories = FieldCache[field.Name];
                FieldExtra extrasCategory = null;
                extrasCategory = categories.GetOrAdd(docFieldVal, (key) =>
                {
                    var newExtra = new FieldExtra()
                    {
                        Key = $"{field.Name}{categories.Count + 1}",
                        Value = docFieldVal
                    };
                    if (field.Extras==null)
                    {
                        field.Extras = new FieldExtras();
                    }
                    field.Extras.Extra.Add(newExtra);
                    return newExtra;
                });
                foreach (var dummy in field.Extras.Extra)
                {
                    doc[dummy.Key] = dummy.Key == extrasCategory.Key ? 1 : 0;
                }
            }
        }
    }

}

