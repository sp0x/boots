using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using Netlyt.Service.Source;

namespace Netlyt.Service.Integration.Encoding
{
    /// <summary>
    /// 
    /// </summary>
    public class FieldEncoder
    {
        private DataIntegration _integration;
        private IEnumerable<FieldDefinition> _encodedFields;
        private IEnumerable<FieldEncoding> _encoders;

        public FieldEncoder(DataIntegration ign)
        {
            _integration = ign;
            _encodedFields = ign.Fields.Where(x => x.DataEncoding != FieldDataEncoding.None);
            _encoders = _encodedFields.GroupBy(x => x.DataEncoding)
                .Select(x => FieldEncoding.Factory.Create(ign, x.Key));
        }

        public class Factory
        {
            public static FieldEncoder Create(DataIntegration ign)
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

        public async Task ApplyToAllFields(CancellationToken? ct)
        {
            foreach (var encoding in _encoders)
            {
                await encoding.ApplyToAllFields(ct);
                if (ct != null && ct.Value.IsCancellationRequested) break;
            }
        }
    }
}