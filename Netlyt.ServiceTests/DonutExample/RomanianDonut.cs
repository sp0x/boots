//Generated on 02-May-18 6:05:04 PM UTC
//Root collection on d10d8ba3-a737-4eb4-9253-f80447b39a11

using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;
using Donut;
using Donut.Data;
using Donut.Caching;

namespace Romanian
{
    public class RomanianDonut : Donutfile<RomanianDonutContext, IntegratedDocument>
    {

        public RomanianDonut(RedisCacher cacher, IServiceProvider serviceProvider) : base(cacher, serviceProvider)
        {
            //ReplayInputOnFeatures = true;
            HasPrepareStage = true;
        }

        protected override void OnCreated()
        {
            base.OnCreated();
            //Perform any initial cleanup
        }

        protected override void OnMetaComplete()
        {
        }

        public override async Task PrepareExtraction()
        {
            var groupKeys = new BsonDocument();
            var groupFields = new BsonDocument();
            var projections = new BsonDocument();
            var pipeline = new List<BsonDocument>();
            var rootCollection = Context.Integration.GetMongoCollection<BsonDocument>();
            groupFields.Merge(BsonDocument.Parse(@"{""f_0"":{""$min"":""$rssi""}}"));

            projections.Merge(new BsonDocument { { "f_0", "$f_0" } });

            groupKeys.Merge(new BsonDocument { { "tsHour", BsonDocument.Parse("{ \"$hour\" : \"$timestamp\" }") } });
            groupKeys.Merge(new BsonDocument { { "tsDayyr", BsonDocument.Parse("{ \"$dayOfYear\" : \"$timestamp\" }") } });
            var grouping = new BsonDocument();
            grouping["_id"] = groupKeys;
            grouping = grouping.Merge(groupFields);
            pipeline.Add(new BsonDocument{
                                        {"$group", grouping}});
            pipeline.Add(new BsonDocument{
                                {"$out", "d10d8ba3-a737-4eb4-9253-f80447b39a11_features"}});
            var aggOptions = new AggregateOptions() { AllowDiskUse = true, BatchSize = 1 };
            var aggregateResult = rootCollection.Aggregate<BsonDocument>(pipeline, aggOptions);

        }

        public override async Task OnFinished()
        {

        }

        public override void ProcessRecord(IntegratedDocument intDoc)
        {
            //Extraction goes in here

        }

    }
}


/* Donut script: 
define RomanianDonut
from Romanian
set f_0 = MIN(Romanian.rssi)
set f_1 = NUM_UNIQUE(Romanian.pm25)
target pm10

*/
