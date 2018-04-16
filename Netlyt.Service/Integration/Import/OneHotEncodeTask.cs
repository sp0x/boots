using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using Netlyt.Service.Source;

namespace Netlyt.Service.Integration.Import
{
    public class OneHotEncodeTask
    {
        private OneHotEncodeTaskOptions _options;
        private DataIntegration _integration;
        private IMongoCollection<BsonDocument> _records;
        private List<FieldDefinition> _oneHotFields;
        private Dictionary<string, ConcurrentDictionary<string, FieldExtra>> _fieldDict;

        public OneHotEncodeTask(OneHotEncodeTaskOptions options)
        {
            _options = options;
            _integration = _options.Integration;
            var collection = _integration.Collection;
            var databaseConfiguration = DBConfig.GetGeneralDatabase();
            var dstCollection = new MongoList(databaseConfiguration, collection);
            _records = dstCollection.Records;
            _oneHotFields = _integration.Fields.Where(x => x.DataEncoding == Source.FieldDataEncoding.OneHot).ToList();
            _fieldDict = new Dictionary<string, ConcurrentDictionary<string, FieldExtra>>();
            foreach (var fld in _oneHotFields)
            {
                if (fld.Extras == null)
                {
                    var dict1 = new ConcurrentDictionary<string, FieldExtra>();
                    _fieldDict.Add(fld.Name, dict1);
                    continue;
                }
                var dict = new ConcurrentDictionary<string, FieldExtra>(fld.Extras.Extra.ToDictionary(x => x.Value));
                _fieldDict[fld.Name] = dict;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Run(CancellationToken? cancellationToken = null)
        {
            if (cancellationToken == null) cancellationToken = CancellationToken.None;
            GetEncodedIntegration();
            await ApplyToAllFields(cancellationToken);
        }

        public async Task ApplyToAllFields(CancellationToken? cancellationToken = null)
        {
            if (cancellationToken == null) cancellationToken = CancellationToken.None;
            foreach (var field in _oneHotFields)
            {
                if (field.Extras == null) continue;
                await ApplyToField(field, cancellationToken);
            }
        }

        public async Task<BulkWriteResult<BsonDocument>> ApplyToField(FieldDefinition field, CancellationToken? cancellationToken = null)
        {
            if (cancellationToken == null) cancellationToken = CancellationToken.None;
            var updateModels = new WriteModel<BsonDocument>[field.Extras.Extra.Count];
            int iModel = 0;
            var dummies = field.Extras.Extra;
            foreach (var column in dummies)
            {
                var otherDummies = dummies.Where(x => x.Key != column.Key);
                //                    var query = Builders<IntegratedDocument>.Filter.And(
                //                        Builders<IntegratedDocument>.Filter.Eq("Document.uuid",
                //                            docFeatures.Document["uuid"].ToString()),
                //                        Builders<IntegratedDocument>.Filter.Eq("Document.noticed_date",
                //                            docFeatures.Document.GetDate("noticed_date")));
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

            var result = await _records.BulkWriteAsync(updateModels, new BulkWriteOptions()
            {
            }, cancellationToken.Value);
            return result;
        }

        public DataIntegration GetEncodedIntegration(bool truncateDestination = false)
        {
            var databaseConfiguration = DBConfig.GetGeneralDatabase();
            var collection = _integration.Collection;
            var dstCollection = new MongoList(databaseConfiguration, collection);

            if (truncateDestination)
            {
                dstCollection.Truncate();
            }
            var records = dstCollection.Records;
            foreach (var oneHotField in _oneHotFields)
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
            return _integration;
        }

        public void Apply(BsonDocument doc)
        {
            foreach (var field in _oneHotFields)
            {
                var docFieldVal = doc[field.Name].ToString();
                var categories = _fieldDict[field.Name];
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

    public class OneHotEncodeTaskOptions
    {
        public DataIntegration Integration { get; set; }
    }
}
