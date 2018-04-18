﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using Netlyt.Service.Source;

namespace Netlyt.Service.Integration.Encoding
{
    public class BinaryCategoryEncoder : FieldEncoder
    {
        public BinaryCategoryEncoder(FieldEncodingOptions options) : base(options, FieldDataEncoding.BinaryIntId)
        {

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
                        Key = EncodeKey(categories.Count + 1),
                        Value = docFieldVal
                    };
                    if (field.Extras == null)
                    {
                        field.Extras = new FieldExtras();
                    }
                    field.Extras.Extra.Add(newExtra);
                    return newExtra;
                });
                doc[field.Name] = int.Parse(extrasCategory.Key);
            }
        }

        protected override string EncodeKey(int i)
        {
            var bvinary = Convert.ToString(i, 2).PadLeft(8, '0');
            bvinary = "1" + bvinary;
            return bvinary;
        }

        public override async Task<BulkWriteResult<BsonDocument>> ApplyToField(FieldDefinition field, CancellationToken? cancellationToken = null)
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
            var result = await Records.BulkWriteAsync(updateModels, new BulkWriteOptions()
            {
            }, cancellationToken.Value);
            return result;
            return null;
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
                int iVariation = fld.Extras.Extra == null ? 1 : fld.Extras.Extra.Count + 1;
                foreach (var uniqueValue in uniqueColumnResults)
                {
                    var columnVal = uniqueValue["_id"].ToString();
                    if (fld.Extras.Extra.Any(y => y.Key == columnVal)) continue;
                    var fieldExtra = new FieldExtra()
                    {
                        Field = fld,
                        Key = EncodeKey(iVariation++),
                        Value = columnVal,
                        Type = FieldExtraType.Dummy
                    };
                    if (fld.Extras == null) fld.Extras = new FieldExtras();
                    fld.Extras.Extra.Add(fieldExtra);
                }
            }
            return Integration;
        }
    }

}
