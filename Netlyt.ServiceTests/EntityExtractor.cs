﻿using System;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Donut;
using Donut.Blocks;
using Donut.Data.Format;
using Donut.IntegrationSource;
using MongoDB.Bson;
using nvoid.db.DB;
using nvoid.extensions;
using nvoid.Integration;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Batching;
using Netlyt.Interfaces.Blocks;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.Service.Integration;
using Netlyt.Service.Integration.Blocks;
using Netlyt.Service.Time;
using Netlyt.ServiceTests.Fixtures;
using Netlyt.ServiceTests.Netinfo;
using Xunit; 

namespace Netlyt.ServiceTests
{
    //Note: All netinfo tests moved to netinfo
    [Collection("Entity Parsers")]
    public class EntityExtractor
    {

        private ConfigurationFixture _config;
        private BsonArray _purchases;
        private BsonArray _purchasesOnHolidays;
        private BsonArray _purchasesBeforeHolidays;
        private BsonArray _purchasesInWeekends;
        private BsonArray _purchasesBeforeWeekends;
        private DateHelper _dateHelper;
        private DynamicContextFactory _contextFactory;
        private ApiService _apiService;
        private IntegrationService _integrationService;
        private ApiAuth _apiAuth;

        public EntityExtractor(ConfigurationFixture fixture)
        {

            _config = fixture;
            _contextFactory = new DynamicContextFactory(() => _config.CreateContext());
            _apiService = fixture.GetService<ApiService>();
            _integrationService = fixture.GetService<IntegrationService>();
            _purchases = new BsonArray();
            _purchasesOnHolidays = new BsonArray();
            _purchasesInWeekends = new BsonArray();
            _purchasesBeforeHolidays = new BsonArray();
            _purchasesBeforeWeekends = new BsonArray();
            _dateHelper = new DateHelper();
            _apiAuth = _apiService.Generate();
            _apiService.Register(_apiAuth);
        }


        //        [Theory]
        //        [InlineData("TestData\\Ebag\\1155\\UUID_1155_all.csv")]
        //        public void ExtractEntityFromSingleFile(string inputFile)
        //        {
        ////            inputFile = Path.Combine(Environment.CurrentDirectory, inputFile);
        ////            var fileSource = FileSource.CreateFromFile(inputFile, new CsvFormatter());
        ////            var type = fileSource.ResolveIntegrationDefinition() as IntegrationTypeDefinition;
        ////            Assert.NotNull(type);
        ////
        ////            var userId = "123123123";
        ////            var userApiId = Guid.NewGuid().ToString();
        ////            var harvester = new Netlyt.Service.Harvester();
        ////            type.APIKey = userId;
        ////            type.SaveType(userApiId);
        ////
        ////            var saver = new MongoSink(userId);
        ////            var featureGen = new NetinfoFeatureGeneratorHelper();
        ////            var featureBlock = featureGen.GetBlock();
        ////            featureBlock.LinkTo(saver, new DataflowLinkOptions{ PropagateCompletion = true}); //We modify the entity to fill all it's data, then generate feature, and then save
        ////            harvester.SetDestination(modifier);
        ////            harvester.AddIntegration(type, fileSource);
        ////            harvester.Run();
        //        } 

        /// <summary>
        /// DEPRECATED
        /// </summary>
        /// <param name="inputDirectory"></param>
        /// <param name="demographySheet"></param>
        [Theory]
        [InlineData(new object[]{"TestData\\Ebag\\1156", "TestData\\Ebag\\demograpy.csv" })]
        public void ExtractEntityFromDirectory(string inputDirectory, string demographySheet)
        {
            inputDirectory = Path.Combine(Environment.CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter<ExpandoObject>());   
            var harvester = new Harvester<IntegratedDocument>();
            var type = harvester.AddIntegrationSource(fileSource, _apiAuth, null); 
            var grouper = new GroupingBlock<IntegratedDocument>(_apiAuth, GroupDocuments, FilterUserCreatedData, AccumulateUserEvent);
            var saver = new MongoSink<IntegratedDocument>(_apiAuth.AppId);
            var demographyImporter = new EntityDataImporter(demographySheet, true);
            //demographyImporter.SetEntityRelation((input, x) => input[0] == x.Document["uuid"]);
            demographyImporter.UseInputKey((input) => input[0] );
            demographyImporter.SetEntityKey((IntegratedDocument input) => input.GetDocument()?["uuid"].ToString());
            demographyImporter.JoinOn(JoinDemography);
            demographyImporter.ReadData();

            //var helper = new CrossSiteAnalyticsHelper(grouper.EntityDictionary); 

            //demographyImporter.Helper = helper;
            //grouper.Helper = helper;
            
            grouper.LinkTo(DataflowBlock.NullTarget<IIntegratedDocument>());
            demographyImporter.LinkTo(DataflowBlock.NullTarget<IntegratedDocument>());

            //var featureGen = new NetinfoFeatureGeneratorHelper() { Helper = helper};
            var featureGen = new NetinfoFeatureGeneratorHelper() { };
            var featureGenBlock = featureGen.GetBlock();
            //featureGen.Helper = helper;readd
            //demographyImporter.LinkTo(featureGen); 

            //featureGenBlock.LinkTo(saver.GetProcessingBlock());

            saver.LinkTo(DataflowBlock.NullTarget<IntegratedDocument>());

            grouper.ContinueWith((grpr) =>
            {
                OnUsersGrouped(demographyImporter, grpr);
            }); //modifier.PostAll);
            demographyImporter.ContinueWith(imp =>
            {
                OnUserDemographyImported(featureGen, grouper);
            });
            
            harvester.SetDestination(grouper);
            harvester.AddIntegration(type, fileSource);
            Task.WaitAll(harvester.Run(), saver.ProcessingCompletion);

            //Task.WaitAll(grouper.Completion, featureGen.Completion, );
            //await saver.ProcessingCompletion;
        }



        private void JoinDemography(string[] demographyFields, IntegratedDocument userDocument)
        { 
            int tAge;
            var userDocumentDocument = userDocument.GetDocument();
            if (int.TryParse(demographyFields[3], out tAge)) userDocumentDocument["age"] = tAge;
            var gender = demographyFields[2];
            if (gender.Length > 0)
            {
                int genderId = 0;
                if (gender == "male") genderId = 1;
                else if (gender == "female") genderId = 2;
                else if (gender == "t") genderId = 1;
                else if (gender == "f") genderId = 2;
                userDocumentDocument["gender"] = genderId; //Gender is t or f anyways
            }
            else
            {
                userDocumentDocument["gender"] = 0;
            }
        }

        private object GroupDocuments(IIntegratedDocument arg)
        {
            var argDocument = arg.GetDocument();
            var uuid = argDocument["uuid"].ToString();
            var date = DateTime.Parse(argDocument["ondate"].ToString());
            return $"{uuid}_{date.Day}"; //_{date.Day}";
        }
         
        private void DumpUsergroupSessionsToCsv(GroupingBlock<IntegratedDocument> usersData)
        {
            var userValues = usersData.EntityDictionary.Values;
            var outputFile = Path.Combine(Environment.CurrentDirectory, "payingBrowsingSessions.csv");
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }
            var outputFs = File.OpenWrite(outputFile);
            var csvWr = new StreamWriter(outputFs);
            csvWr.WriteLine("uuid,domain,duration,ondate");
            try
            {
                foreach (var userDayInfoPairs in usersData.EntityDictionary)
                {
                    try
                    {
                        var userGroup = usersData.EntityDictionary[userDayInfoPairs.Key];
                        var userDocument = userGroup.GetDocument();
                        var userIsPaying = userDocument.Contains("is_paying") &&
                                           userDocument["is_paying"].AsInt32 == 1;
                        //We're only interested in paying users
                        if (!userIsPaying) continue;
                        var uuid = userDocument["uuid"].ToString();
                        userDocument["events"] =
                            ((BsonArray) userDocument["events"])
                            .OrderBy(x => DateTime.Parse(x["ondate"].ToString()))
                            .ToBsonArray(); 
//                        foreach (DomainUserSession visitSession in NetinfoDonutfile.GetWebSessions(userGroup))
//                        {
//                            var newLine = string.Format("{0},{1},{2},{3}", uuid, visitSession.Domain,
//                                visitSession.Duration.TotalSeconds, visitSession.Visited);
//                            csvWr.WriteLine(newLine);
//                        }
                    }
                    catch (Exception ex2)
                    {
                        Trace.WriteLine(ex2.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            outputFs.Close();
        }

        /// <summary>
        /// Invoked after users are finished being grouped
        /// </summary>
        /// <param name="dataImporter"></param>
        /// <param name="fromBlock"></param>
        private void OnUsersGrouped(EntityDataImporter dataImporter, IFlowBlock fromBlock)
        {
            var grouper = fromBlock as GroupingBlock<IntegratedDocument>;
            var userValues = grouper.EntityDictionary.Values; 
            //var today = DateTime.Today;
            var dateHelper = new DateHelper();
            double max_time_spent_by_any_paying_user_ebag = 0;
                //dataImporter.Helper.GetLongestVisitPurchaseDuration("ebag.bg", "payments/finish");
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            Calendar cal = dfi.Calendar;

            var purchasesCount = (double)_purchases.Count;
            if (purchasesCount == 0) purchasesCount = 1;
            var prob_buy_is_holiday = (double)_purchasesOnHolidays.Count / purchasesCount;
            var prob_buy_is_before_holiday = (double)_purchasesBeforeHolidays.Count / purchasesCount;
            var prop_buy_is_weekend = (double)_purchasesInWeekends.Count / purchasesCount;
            foreach (var userDayInfoPairs in grouper.EntityDictionary)
            {
                var user = grouper.EntityDictionary[userDayInfoPairs.Key];
                var userDocument = user.GetDocument();
                userDocument["events"] =
                    ((BsonArray)userDocument["events"])
                    .OrderBy(x => DateTime.Parse(x["ondate"].ToString()))
                    .ToBsonArray();
                var g_timestamp = userDocument["noticed_date"].ToUniversalTime();
                var weekstart = g_timestamp.StartOfWeek(DayOfWeek.Monday);
                g_timestamp = weekstart;
                userDocument["g_timestamp"] = g_timestamp;

                //var args = new FindAndModifyArgs();
                //args.Query = Query.EQ("Document.uuid", uuid);
                //args.Update = Update.Set("Document.g_timestamp", g_timestamp);
                //mongoClient.FindAndModify(args); 

                userDocument["max_time_spent_by_any_paying_user_ebag"] = max_time_spent_by_any_paying_user_ebag; 

                userDocument["prob_buy_is_holiday"] = prob_buy_is_holiday;
                userDocument["prob_buy_is_before_holiday"] = prob_buy_is_before_holiday;
                userDocument["prop_buy_is_weekend"] = prop_buy_is_weekend;
            }
            
            dataImporter.PostAll(userValues);
        }

        private void OnUserDemographyImported(NetinfoFeatureGeneratorHelper gen, GroupingBlock<IntegratedDocument> usersData)
        { 
            var userValues = usersData.EntityDictionary.Values;
            //gen.Helper.GroupDemographics(); //readd
            var featureblock = gen.GetBlock();
            featureblock.PostAll(userValues); 
        }

        /// <summary>
        /// Format the initial user data
        /// </summary>
        /// <param name="obj"></param>
        private void FilterUserCreatedData(IIntegratedDocument obj)
        {
            var objDocument = obj.GetDocument();
            var date = DateTime.Parse(objDocument["ondate"].ToString());
            objDocument.Remove("event_id");
            objDocument.Remove("ondate");
            objDocument.Remove("value");
            objDocument.Remove("type");
            objDocument["noticed_date"] = date;
            //obj.Document["noticed_year"] = date.Year;
        }

        /// <summary>
        /// Format the input element that should be added
        /// </summary>
        /// <param name="accumulator"></param>
        /// <param name="newEntry"></param>
        private object AccumulateUserEvent(IIntegratedDocument accumulator, BsonDocument newEntry)
        {
            var value = newEntry["value"]?.ToString();
            var onDate = newEntry["ondate"].ToString();
            var newElement = new
            {
                ondate = onDate,
                event_id = newEntry.GetInt("event_id"),
                type = newEntry.GetInt("type"),
                value = value
            }.ToBsonDocument();

            accumulator.AddDocumentArrayItem("events", newElement);
            if (value.Contains("payments/finish") && Donut.Blocks.Extensions.ToHostname(value).Contains("ebag.bg"))
            {
                var dateTime = DateTime.Parse(onDate);
                if (DateHelper.IsHoliday(dateTime))
                {
                    _purchasesOnHolidays.Add(newElement);
                }
                else if (DateHelper.IsHoliday(dateTime.AddDays(1)))
                {
                    _purchasesBeforeHolidays.Add(newElement);
                }
                else if (dateTime.DayOfWeek == DayOfWeek.Friday)
                {
                    _purchasesBeforeWeekends.Add(newElement);
                }
                else if (dateTime.DayOfWeek > DayOfWeek.Friday)
                {
                    _purchasesInWeekends.Add(newElement);
                }
                _purchases.Add(newElement); 
                accumulator["is_paying"] = 1;
                //bsonDocument["is_paying"] = 1;
            }
            return newElement;
        }
         
    }
}
