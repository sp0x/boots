using System;
using System.Collections.Generic;
using Donut;
using MongoDB.Bson;
using Donut.FeatureGeneration;

namespace Romanian
{
    public class RomanianFeatureGenerator : DonutFeatureEmitter<RomanianDonut, RomanianDonutContext, IntegratedDocument>
    {
        public RomanianFeatureGenerator(RomanianDonut donut) : base(donut)
        {

        }

        public override IEnumerable<KeyValuePair<string, object>> GetFeatures(IntegratedDocument intDoc)
        {
            Func<string, object, KeyValuePair<string, object>> pair = (x, y) => new KeyValuePair<string, object>(x, y);
            BsonDocument intDocDocument = intDoc.GetDocument();
            var doc = intDocDocument;



            yield break;
        }
    }
}
