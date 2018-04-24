using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using Netlyt.Interfaces;
using Netlyt.Service.Integration.Import;
using Netlyt.Service.Source;

namespace Netlyt.Service.Integration.Encoding
{
    public abstract class FieldEncoding : IFieldEncoder
    {
        private FieldEncodingOptions _options;
        private IIntegration _integration;
        private IMongoCollection<BsonDocument> _records;
        private List<IFieldDefinition> _targetFields;
        private Dictionary<string, ConcurrentDictionary<string, IFieldExtra>> _fieldDict;
        public FieldDataEncoding Encoding { get; set; }
        protected List<IFieldDefinition> TargetFields => _targetFields;
        protected IIntegration Integration => _integration;
        protected IMongoCollection<BsonDocument> Records => _records;
        protected Dictionary<string, ConcurrentDictionary<string, IFieldExtra>> FieldCache
        {
            get { return _fieldDict; }
            set { _fieldDict = value; }
        }
        public FieldEncoding(FieldEncodingOptions options, FieldDataEncoding encoding)
        {
            this.Encoding = encoding;
            _options = options;
            _integration = _options.Integration;
            var collection = _integration.Collection;
            var databaseConfiguration = DBConfig.GetGeneralDatabase();
            var dstCollection = new MongoList(databaseConfiguration, collection);
            _records = dstCollection.Records;
            _targetFields = _integration.Fields.Where(x => x.DataEncoding == encoding).ToList();
            _fieldDict = new Dictionary<string, ConcurrentDictionary<string, IFieldExtra>>();
            foreach (var fld in TargetFields)
            {
                if (fld.Extras == null)
                {
                    var dict1 = new ConcurrentDictionary<string, IFieldExtra>();
                    _fieldDict.Add(fld.Name, dict1);
                    continue;
                }
                var dict = new ConcurrentDictionary<string, IFieldExtra>(fld.Extras.Extra.ToDictionary(x => x.Value));
                _fieldDict[fld.Name] = dict;
            }
        }

        public abstract void Apply(BsonDocument doc);
        public async Task ApplyToAllFields(CancellationToken? cancellationToken = null)
        {
            if (cancellationToken == null) cancellationToken = CancellationToken.None;
            foreach (var field in TargetFields)
            {
                if (field.Extras == null) continue;
                await ApplyToField(field, cancellationToken);
            }
        }
        public abstract Task<BulkWriteResult<BsonDocument>> ApplyToField(IFieldDefinition field, CancellationToken? cancellationToken = null);
        public abstract IIntegration GetEncodedIntegration(bool truncateDestination = false);
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
        protected virtual string EncodeKey(int i)
        {
            return null;
        }

        public class Factory
        {
            public static FieldEncoding Create(DataIntegration integration, FieldDataEncoding fldDataEncoding)
            {
                var ops = new FieldEncodingOptions { Integration = integration };
                if (fldDataEncoding==FieldDataEncoding.OneHot)
                {
                    return new OneHotEncoding(ops);
                }
                else if (fldDataEncoding==FieldDataEncoding.BinaryIntId)
                {
                    return new BinaryCategoryEncoding(ops);
                }
                else
                {
                    return null;
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

        public virtual IEnumerable<string> GetEncodedFieldNames(IFieldDefinition fld)
        {
            yield break;
        }
    }
}