using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Donut.Data;
using Donut.Integration;
using Donut.Source;
using MongoDB.Bson;
using MongoDB.Driver;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;

namespace Donut.Encoding
{
    public abstract class FieldEncoding : IFieldEncoder
    {
        private FieldEncodingOptions _options;
        private IIntegration _integration;
        private List<FieldDefinition> _targetFields;
        private Dictionary<string, ConcurrentDictionary<string, FieldExtra>> _fieldDict;
        public int Limit { get; set; } = 8;
        public FieldDataEncoding Encoding { get; set; }
        public IFieldExtraRepository ExtrasRepository { get; private set; }
        protected List<FieldDefinition> TargetFields => _targetFields;
        protected IIntegration Integration => _integration;
        /// <summary>
        /// TODO: Manage the cache, because it might get too large after being used for long..
        /// </summary>
        protected Dictionary<string, ConcurrentDictionary<string, FieldExtra>> FieldCache
        {
            get { return _fieldDict; }
            set { _fieldDict = value; }
        }
        public FieldEncoding(FieldEncodingOptions options, FieldDataEncoding encoding)
        {
            this.Encoding = encoding;
            _options = options;
            _integration = _options.Integration;
            //var collection = MongoHelper.GetCollection(_integration.Collection);
            _targetFields = _integration.Fields.Where(x => x.DataEncoding == encoding).ToList();
            _fieldDict = new Dictionary<string, ConcurrentDictionary<string, FieldExtra>>();
            foreach (var fld in TargetFields)
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

        public abstract void Apply(BsonDocument doc);

        /// <summary>
        /// Applies the encoding to all target fields.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task ApplyToAllFields(IMongoCollection<BsonDocument> collection, 
            CancellationToken? cancellationToken = null)
        {
            if (cancellationToken == null) cancellationToken = CancellationToken.None;
            foreach (var field in TargetFields)
            {
                if (field.Extras == null) continue;
                await ApplyToField(field, collection, cancellationToken);
            }
        }
        public abstract Task<BulkWriteResult<BsonDocument>> ApplyToField(
            IFieldDefinition field,
            IMongoCollection<BsonDocument> collection,
            CancellationToken? cancellationToken = null);
        public abstract IIntegration GetEncodedIntegration(bool truncateDestination = false);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Run(IMongoCollection<BsonDocument> collection, CancellationToken? cancellationToken = null)
        {
            if (cancellationToken == null) cancellationToken = CancellationToken.None;
            GetEncodedIntegration();
            await ApplyToAllFields(collection, cancellationToken);
        }
        protected virtual string EncodeKey(int i)
        {
            return null;
        }

        public class Factory
        {
            public static FieldEncoding Create(IIntegration integration, FieldDataEncoding fldDataEncoding)
            {
                //TODO: Replace this with attributes.
                var ops = new FieldEncodingOptions { Integration = integration };
                if (fldDataEncoding==FieldDataEncoding.OneHot)
                {
                    return new OneHotEncoding(ops);
                }
                else if (fldDataEncoding==FieldDataEncoding.BinaryIntId)
                {
                    return new BinaryCategoryEncoding(ops);
                }
                else if (fldDataEncoding == FieldDataEncoding.Id)
                {
                    return new IdEncoding(ops);
                }
                else
                {
                    throw new NotImplementedException($"Field encoding not implemented: {fldDataEncoding}");
                }
            }
        }


        public IEnumerable<string> GetFieldNames(IFieldDefinition fld)
        {
            if (fld.Extras == null)
            {
                yield return fld.Name;
                yield break;
            }
            var names = GetEncodedFieldNames(fld);
            if (!names.Any())
            {
                yield return fld.Name;
                yield break;
            }
            foreach (var name in names)
            {
                yield return name;
            }
        }

        public IEnumerable<KeyValuePair<string, int>> GetFieldpairs(BsonDocument document)
        {
            foreach (var field in TargetFields)
            {
                if (!document.Contains(field.Name)) continue;
                var pairs = GetDecodedFieldpairs(field, document);
                foreach (var pair in pairs)
                {
                    yield return pair;
                }
            }
        }

        public abstract IEnumerable<KeyValuePair<string, int>> GetDecodedFieldpairs(FieldDefinition field,
            BsonDocument document);

        public virtual IEnumerable<string> GetEncodedFieldNames(IFieldDefinition fld)
        {
            yield break;
        }

        public static void SetEncoding(DataIntegration ign, FieldDefinition targetField, Type opsEncoding)
        {
            var ctor = opsEncoding.GetConstructor(new Type[] {typeof(FieldEncodingOptions)});
            FieldEncoding encoding = ctor.Invoke(new object[]{new FieldEncodingOptions
            {
                Integration = ign
            }}) as FieldEncoding;
            if (encoding == null)
            {
                throw new Exception($"Could not create encoding for field: {targetField.Name}");
            }
            targetField.DataEncoding = encoding.Encoding;
            targetField.Extras = new FieldExtras();
            //targetField.Extras.Field = targetField;
        }


        public void DecodeFields(BsonDocument document)
        {
            foreach (var field in TargetFields)
            {
                if (!document.Contains(field.Name)) continue;
                var encValue = document[field.Name];
                var decValue = DecodeField(field, encValue);
                document[field.Name] = decValue;
            }
        }

        public void SetExtrasRepository(IFieldExtraRepository fieldsRepo)
        {
            this.ExtrasRepository = fieldsRepo;
        }

        public abstract string DecodeField(FieldDefinition field, BsonValue value);
    }
}