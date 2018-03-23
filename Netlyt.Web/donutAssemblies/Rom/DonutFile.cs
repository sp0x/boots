using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using nvoid.db.Caching;
using nvoid.db.DB;
using nvoid.extensions;
using Netlyt.Service.Donut;
using Netlyt.Service.Integration; 
using Netlyt.Service.Time;
using System.Threading.Tasks; 

namespace Rom
{
    public class RomDonut : Donutfile<RomDonutContext>
    { 

        public RomDonut(RedisCacher cacher, IServiceProvider serviceProvider) : base(cacher, serviceProvider)
        {
            //ReplayInputOnFeatures = true;
        }
		 
        protected override void OnCreated()
        {
            base.OnCreated(); 
			//Perform any initial cleanup
        }

        protected override void OnMetaComplete()
        {
        }

		public override async Task OnFinished()
		{
			var groupKeys = new BsonDocument();
			var groupFields = new BsonDocument();
			var projections = new BsonDocument();
			var pipeline = new List<BsonDocument>(); 

			var recRom = this.Context.Rom.Records;
groupFields["f_0"] = new BsonDocument { { "$first", "$pm10" } };
groupFields["f_1"] = BsonDocument.Parse("{ \"$sum\" : \"$humidity\" }");
groupFields["f_2"] = BsonDocument.Parse("{ \"$sum\" : \"$latitude\" }");
groupFields["f_3"] = BsonDocument.Parse("{ \"$sum\" : \"$longitude\" }");
groupFields["f_4"] = BsonDocument.Parse("{ \"$sum\" : \"$pm25\" }");
groupFields["f_5"] = BsonDocument.Parse("{ \"$sum\" : \"$pressure\" }");
groupFields["f_6"] = BsonDocument.Parse("{ \"$sum\" : \"$rssi\" }");
groupFields["f_7"] = BsonDocument.Parse("{ \"$sum\" : \"$temperature\" }");
groupFields["f_8"] = BsonDocument.Parse("{ \"$stdDevSamp\" : \"$humidity\" }");
groupFields["f_9"] = BsonDocument.Parse("{ \"$stdDevSamp\" : \"$latitude\" }");
groupFields["f_10"] = BsonDocument.Parse("{ \"$stdDevSamp\" : \"$longitude\" }");
groupFields["f_11"] = BsonDocument.Parse("{ \"$stdDevSamp\" : \"$pm25\" }");
groupFields["f_12"] = BsonDocument.Parse("{ \"$stdDevSamp\" : \"$pressure\" }");
groupFields["f_13"] = BsonDocument.Parse("{ \"$stdDevSamp\" : \"$rssi\" }");
groupFields["f_14"] = BsonDocument.Parse("{ \"$stdDevSamp\" : \"$temperature\" }");
groupFields["f_15"] = BsonDocument.Parse("{ \"$max\" : \"$humidity\" }");
groupFields["f_16"] = BsonDocument.Parse("{ \"$max\" : \"$latitude\" }");
groupFields["f_17"] = BsonDocument.Parse("{ \"$max\" : \"$longitude\" }");
groupFields["f_18"] = BsonDocument.Parse("{ \"$max\" : \"$pm25\" }");
groupFields["f_19"] = BsonDocument.Parse("{ \"$max\" : \"$pressure\" }");
groupFields["f_20"] = BsonDocument.Parse("{ \"$max\" : \"$rssi\" }");
groupFields["f_21"] = BsonDocument.Parse("{ \"$max\" : \"$temperature\" }");
groupFields["f_22"] = new BsonDocument{{ "$first", "$humidity" }};
groupFields["f_23"] = new BsonDocument{{ "$first", "$latitude" }};
groupFields["f_24"] = new BsonDocument{{ "$first", "$longitude" }};
groupFields["f_25"] = new BsonDocument{{ "$first", "$pm25" }};
groupFields["f_26"] = new BsonDocument{{ "$first", "$pressure" }};
groupFields["f_27"] = new BsonDocument{{ "$first", "$rssi" }};
groupFields["f_28"] = new BsonDocument{{ "$first", "$temperature" }};
groupFields["f_29"] = BsonDocument.Parse("{ \"$min\" : \"$humidity\" }");
groupFields["f_30"] = BsonDocument.Parse("{ \"$min\" : \"$latitude\" }");
groupFields["f_31"] = BsonDocument.Parse("{ \"$min\" : \"$longitude\" }");
groupFields["f_32"] = BsonDocument.Parse("{ \"$min\" : \"$pm25\" }");
groupFields["f_33"] = BsonDocument.Parse("{ \"$min\" : \"$pressure\" }");
groupFields["f_34"] = BsonDocument.Parse("{ \"$min\" : \"$rssi\" }");
groupFields["f_35"] = BsonDocument.Parse("{ \"$min\" : \"$temperature\" }");
groupFields["f_36"] = BsonDocument.Parse("{ \"$avg\" : \"$humidity\" }");
groupFields["f_37"] = BsonDocument.Parse("{ \"$avg\" : \"$latitude\" }");
groupFields["f_38"] = BsonDocument.Parse("{ \"$avg\" : \"$longitude\" }");
groupFields["f_39"] = BsonDocument.Parse("{ \"$avg\" : \"$pm25\" }");
groupFields["f_40"] = BsonDocument.Parse("{ \"$avg\" : \"$pressure\" }");
groupFields["f_41"] = BsonDocument.Parse("{ \"$avg\" : \"$rssi\" }");
groupFields["f_42"] = BsonDocument.Parse("{ \"$avg\" : \"$temperature\" }");
groupFields["f_43"] = new BsonDocument{{ "$first", "$timestamp" }};
groupFields["f_44"] = new BsonDocument{{ "$first", "$timestamp" }};
groupFields["f_45"] = new BsonDocument{{ "$first", "$timestamp" }};
groupFields["f_46"] = new BsonDocument{{ "$first", "$timestamp" }};
var idSubKey1 = new BsonDocument { { "idKey", "$_id" } };
var idSubKey2 = new BsonDocument { { "tsKey", new BsonDocument{{ "$dayOfYear", "$timestamp"}} } };
groupKeys.Merge(idSubKey1);
groupKeys.Merge(idSubKey2);
var grouping = new BsonDocument();
grouping["_id"] = groupKeys;
grouping = grouping.Merge(groupFields);
pipeline.Add(new BsonDocument{
                                        {"$group", grouping}});
pipeline.Add(new BsonDocument{
                                {"$out", "d2eae8b6-80ce-445e-b111-8013cb6975b1_features"}});
var aggregateResult = recRom.Aggregate<BsonDocument>(pipeline);


		}

		public override void ProcessRecord(IntegratedDocument intDoc){
			//Extraction goes in here
			
		}

	}
}