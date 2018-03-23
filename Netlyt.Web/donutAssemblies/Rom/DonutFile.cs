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
			var pipeline = new BsonDocument[] {}; 

			var recRom = this.Context.Rom.Records;
groupFields["f_0"] = "$pm10";
groupFields["f_1"] = "{ \"$sum\" : \"$humidity\" }";
groupFields["f_2"] = "{ \"$sum\" : \"$latitude\" }";
groupFields["f_3"] = "{ \"$sum\" : \"$longitude\" }";
groupFields["f_4"] = "{ \"$sum\" : \"$pm25\" }";
groupFields["f_5"] = "{ \"$sum\" : \"$pressure\" }";
groupFields["f_6"] = "{ \"$sum\" : \"$rssi\" }";
groupFields["f_7"] = "{ \"$sum\" : \"$temperature\" }";
groupFields["f_8"] = "{ \"$stdDevSamp\" : \"$humidity\" }";
groupFields["f_9"] = "{ \"$stdDevSamp\" : \"$latitude\" }";
groupFields["f_10"] = "{ \"$stdDevSamp\" : \"$longitude\" }";
groupFields["f_11"] = "{ \"$stdDevSamp\" : \"$pm25\" }";
groupFields["f_12"] = "{ \"$stdDevSamp\" : \"$pressure\" }";
groupFields["f_13"] = "{ \"$stdDevSamp\" : \"$rssi\" }";
groupFields["f_14"] = "{ \"$stdDevSamp\" : \"$temperature\" }";
groupFields["f_15"] = "{ \"$max\" : \"$humidity\" }";
groupFields["f_16"] = "{ \"$max\" : \"$latitude\" }";
groupFields["f_17"] = "{ \"$max\" : \"$longitude\" }";
groupFields["f_18"] = "{ \"$max\" : \"$pm25\" }";
groupFields["f_19"] = "{ \"$max\" : \"$pressure\" }";
groupFields["f_20"] = "{ \"$max\" : \"$rssi\" }";
groupFields["f_21"] = "{ \"$max\" : \"$temperature\" }";
groupFields["f_22"] = new BsonDocument{{ "$first", "$humidity" }};
groupFields["f_23"] = new BsonDocument{{ "$first", "$latitude" }};
groupFields["f_24"] = new BsonDocument{{ "$first", "$longitude" }};
groupFields["f_25"] = new BsonDocument{{ "$first", "$pm25" }};
groupFields["f_26"] = new BsonDocument{{ "$first", "$pressure" }};
groupFields["f_27"] = new BsonDocument{{ "$first", "$rssi" }};
groupFields["f_28"] = new BsonDocument{{ "$first", "$temperature" }};
groupFields["f_29"] = "{ \"$min\" : \"$humidity\" }";
groupFields["f_30"] = "{ \"$min\" : \"$latitude\" }";
groupFields["f_31"] = "{ \"$min\" : \"$longitude\" }";
groupFields["f_32"] = "{ \"$min\" : \"$pm25\" }";
groupFields["f_33"] = "{ \"$min\" : \"$pressure\" }";
groupFields["f_34"] = "{ \"$min\" : \"$rssi\" }";
groupFields["f_35"] = "{ \"$min\" : \"$temperature\" }";
groupFields["f_36"] = "{ \"$avg\" : \"$humidity\" }";
groupFields["f_37"] = "{ \"$avg\" : \"$latitude\" }";
groupFields["f_38"] = "{ \"$avg\" : \"$longitude\" }";
groupFields["f_39"] = "{ \"$avg\" : \"$pm25\" }";
groupFields["f_40"] = "{ \"$avg\" : \"$pressure\" }";
groupFields["f_41"] = "{ \"$avg\" : \"$rssi\" }";
groupFields["f_42"] = "{ \"$avg\" : \"$temperature\" }";
projections["f_43"] = "{ \"$dayOfMonth\" : \"$first_Rom_time\" }";
projections["f_44"] = "{ \"$year\" : \"$first_Rom_time\" }";
projections["f_45"] = "{ \"$month\" : \"$first_Rom_time\" }";
projections["f_46"] = "{ \"$dayOfWeek\" : \"$first_Rom_time\" }";
groupKeys["f_47"] = "";
groupKeys["f_48"] = "";
groupKeys["f_49"] = "";
groupKeys["f_50"] = "";
pipeline.Add(new BsonDocument{
                                        {"$group", new BsonDocument{ groupKeys , groupFields}}
                                        });
pipeline.Add(new BsonDocument{
                                        {"$project", projections} });
pipeline.Add(new BsonDocument{
                                {"$out", "2c19b89d-a052-4701-9f8d-d7d9c4a77a75_features"}});
var aggregateResult = recRom.Aggregate<BsonDocument>(pipeline);


		}

		public override void ProcessRecord(IntegratedDocument intDoc){
			//Extraction goes in here
			
		}

	}
}