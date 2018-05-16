﻿using System.Collections.Generic;
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

        public async Task ApplyToAllFields(IMongoCollection<BsonDocument> collection, CancellationToken? ct)
        {
            foreach (var encoding in _encoders)
            {
                await encoding.ApplyToAllFields(collection, ct);
                if (ct != null && ct.Value.IsCancellationRequested) break;
            }
        }

    }
}