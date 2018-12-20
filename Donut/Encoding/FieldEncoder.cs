using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Donut.Integration;
using Donut.Source;
using MongoDB.Bson;
using MongoDB.Driver;
using Netlyt.Interfaces;

namespace Donut.Encoding
{
    /// <summary>
    /// 
    /// </summary>
    public class FieldEncoder
    {
        private IIntegration _integration;
        private IEnumerable<IFieldDefinition> _encodedFields;
        private IEnumerable<FieldEncoding> _encoders;

        public FieldEncoder(IIntegration ign)
        {
            _integration = ign;
            _encodedFields = ign.Fields.Where(x => x.DataEncoding != FieldDataEncoding.None);
            _encoders = _encodedFields.GroupBy(x => x.DataEncoding)
                .Select(x => FieldEncoding.Factory.Create(ign, x.Key)).ToList();
        }

        public class Factory
        {
            public static FieldEncoder Create(IIntegration ign)
            {
                var encoder = new FieldEncoder(ign);
                return encoder;
            }
        }

        public void Apply(BsonDocument doc)
        {
            foreach (var encoding in _encoders)
            {
                encoding.Apply(doc);
            }
        }
        public void Apply<TData>(TData doc) where TData : class, IIntegratedDocument
        {
            var internalDoc = doc.Document?.Value;
            if (internalDoc == null) return;
            foreach (var encoding in _encoders)
            {
                encoding.Apply(internalDoc);
            }
        }

        public IEnumerable<KeyValuePair<string, int>> GetFieldpairs<TData>(TData doc) where TData : class, IIntegratedDocument
        {
            var internalDoc = doc.Document?.Value;
            if (internalDoc == null) yield break;
            foreach (var encoding in _encoders)
            {
                var fields = encoding.GetFieldpairs(internalDoc).ToList();
                foreach (var field in fields)
                {
                    yield return field;
                }
            }
        }

        public IEnumerable<KeyValuePair<string, int>> GetFieldpairs(BsonDocument doc)
        {
            if (doc == null) yield break;
            foreach (var encoding in _encoders)
            {
                var fields = encoding.GetFieldpairs(doc).ToList();
                foreach (var field in fields)
                {
                    yield return field;
                }
            }
        }

        /// <summary>
        /// Applies all encoders to all fields in a collection
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task ApplyToAllFields(IMongoCollection<BsonDocument> collection, CancellationToken? ct)
        {
            foreach (var encoding in _encoders)
            {
                await encoding.ApplyToAllFields(collection, ct);
                if (ct != null && ct.Value.IsCancellationRequested) break;
            }
        }

        public virtual void DecodeFields<TData>(TData f) where TData : class, IIntegratedDocument
        {
            var internalDoc = f.Document?.Value;
            if (internalDoc == null) return;
            var fields = GetFieldpairs(f).ToList();
            foreach (var field in fields)
            {
                internalDoc[field.Key] = field.Value;
            }
        }

        public virtual void DecodeFields(BsonDocument document, IFieldExtraRepository fieldsRepo)
        {
            if (document == null) return;
            foreach (var encoding in _encoders)
            {
                encoding.SetExtrasRepository(fieldsRepo);
                encoding.DecodeFields(document);
            }

        }

    }
}