﻿//Generated on 07-May-18 2:41:46 PM UTC
//Root collection on 92347579-cc06-426c-924d-716ca29cc4d4

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
                                {"$out", "92347579-cc06-426c-924d-716ca29cc4d4_features"}});
            var aggOptions = new AggregateOptions() { AllowDiskUse = true, BatchSize = 1 };
            var aggregateResult = rootCollection.Aggregate<BsonDocument>(pipeline, aggOptions);

        }

        public override async Task OnFinished()
        {

        }

        public override async Task CompleteExtraction()
        {
            Context.GetSetSize("nu_Romanian_583730344");
            /**
             * TODO:
             * Get the group key values that were generated by this run
             * Get the meta category id for each of the groups
             * Update each group with the extracted features
             *
             * mmmmmmmmmmmmmmmmmmmxx+++++++++++++++++++++++++++++++++++++++++ddddddddddddddddddddddd
             */
            //Example:
            //var groupId = 3;
            //NUM UNIQUE
            //var cacheSize = Context.GetSetSize($"{groupId}:nu_Romanian_583730344")
            //var feature = define a feature with key num_unique (feature name) = cacheSize
            //
        }

        public override void ProcessRecord(IntegratedDocument document)
        {
            //Extraction goes in here
            var aggKeyBuff = new Dictionary<string, object>();
            Func<BsonValue, System.Int32> aggKey0_fn = x => x.AsDateTime.Hour;
            aggKeyBuff["tsHour"] = aggKey0_fn(document["timestamp"]);
            Func<BsonValue, System.Int32> aggKey1_fn = x => x.AsDateTime.DayOfYear;
            aggKeyBuff["tsDayyr"] = aggKey1_fn(document["timestamp"]);
            var groupKey = Context.AddMetaGroup(aggKeyBuff);

            var nu_Romanian_583730344_cat = groupKey;
            var nu_Romanian_583730344_val = document["pm25"].ToString();
            Context.AddEntityMetaCategory("nu_Romanian_583730344", nu_Romanian_583730344_cat, nu_Romanian_583730344_val, true);
            //Register group key

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