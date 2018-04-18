using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using Netlyt.Service.Integration.Import;
using Netlyt.Service.Source;

namespace Netlyt.Service.Integration.Encoding
{
    public abstract class FieldEncoder : IFieldEncoder
    {
        private FieldEncodingOptions _options;
        private DataIntegration _integration;
        private IMongoCollection<BsonDocument> _records;
        private List<FieldDefinition> _targetFields;
        private Dictionary<string, ConcurrentDictionary<string, FieldExtra>> _fieldDict;
        public FieldDataEncoding Encoding { get; set; }
        protected List<FieldDefinition> TargetFields => _targetFields;
        protected DataIntegration Integration => _integration;
        protected IMongoCollection<BsonDocument> Records => _records;
        protected Dictionary<string, ConcurrentDictionary<string, FieldExtra>> FieldCache
        {
            get { return _fieldDict; }
            set { _fieldDict = value; }
        }
        public FieldEncoder(FieldEncodingOptions options, FieldDataEncoding encoding)
        {
            this.Encoding = encoding;
            _options = options;
            _integration = _options.Integration;
            var collection = _integration.Collection;
            var databaseConfiguration = DBConfig.GetGeneralDatabase();
            var dstCollection = new MongoList(databaseConfiguration, collection);
            _records = dstCollection.Records;
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
        public async Task ApplyToAllFields(CancellationToken? cancellationToken = null)
        {
            if (cancellationToken == null) cancellationToken = CancellationToken.None;
            foreach (var field in TargetFields)
            {
                if (field.Extras == null) continue;
                await ApplyToField(field, cancellationToken);
            }
        }
        public abstract Task<BulkWriteResult<BsonDocument>> ApplyToField(FieldDefinition field, CancellationToken? cancellationToken = null);
        public abstract DataIntegration GetEncodedIntegration(bool truncateDestination = false);
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
            public static FieldEncoder Create(DataIntegration integration)
            {
                var oneHotFields = integration.Fields.Any(x => x.DataEncoding == FieldDataEncoding.OneHot);
                var biiFields = integration.Fields.Any(x => x.DataEncoding == FieldDataEncoding.BinaryIntId);
                var ops = new FieldEncodingOptions { Integration = integration };
                if (oneHotFields)
                {
                    return new OneHotEncoder(ops);
                }
                else if (biiFields)
                {
                    return new BinaryCategoryEncoder(ops);
                }
                else
                {
                    return null;
                }
            }
        }

        
    }
}