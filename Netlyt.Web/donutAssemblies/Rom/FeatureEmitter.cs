using System;
using System.Collections.Generic;
using System.Text;  
using System.Linq;
using System.Threading.Tasks.Dataflow;
using MongoDB.Bson;
using nvoid.extensions;
using Netlyt.Service;
using Netlyt.Service.Integration; 
using Netlyt.Service.Time;

namespace Rom
{
    public class FeatureGenerator : DonutFeatureEmitter<RomDonut, RomDonutContext>
    {
		public FeatureGenerator(RomDonut donut) : base(donut)
		{
			
		}

		public override IEnumerable<KeyValuePair<string, object>> GetFeatures(IntegratedDocument intDoc)
		{
			Func<string, object, KeyValuePair<string, object>> pair = (x, y) => new KeyValuePair<string, object>(x, y);
			BsonDocument intDocDocument = intDoc.GetDocument();
            var doc = intDocDocument;

			yield return pair("f_0", doc["pm10"]);


			yield break;
		}
    }
}
