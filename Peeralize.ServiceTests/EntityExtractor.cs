using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MongoDB.Bson;
using MongoDB.Driver; 
using nvoid.db.Extensions;
using nvoid.extensions;
using Peeralize.Service;
using Peeralize.Service.Format;
using Peeralize.Service.Integration;
using Peeralize.Service.Integration.Blocks;
using Peeralize.Service.IntegrationSource;
using Peeralize.Service.Source;
using Peeralize.Service.Time;
using Peeralize.ServiceTests.IntegrationSource;
using Xunit;
using Harvester = Peeralize.Service.Harvester;

namespace Peeralize.ServiceTests
{
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

        public EntityExtractor(ConfigurationFixture fixture)
        {
            _config = fixture;
            _purchases = new BsonArray();
            _purchasesOnHolidays = new BsonArray();
            _purchasesInWeekends = new BsonArray();
            _purchasesBeforeHolidays = new BsonArray();
            _purchasesBeforeWeekends = new BsonArray();
            _dateHelper = new DateHelper();
        }



        [Theory]
        [InlineData("TestData\\Ebag\\1155\\UUID_1155_all.csv")]
        public void ExtractEntityFromSingleFile(string inputFile)
        {
            inputFile = Path.Combine(Environment.CurrentDirectory, inputFile);
            var fileSource = FileSource.CreateFromFile(inputFile, new CsvFormatter());
            var type = fileSource.GetTypeDefinition() as IntegrationTypeDefinition;
            Assert.NotNull(type);

            var userId = "123123123";
            var userApiId = Guid.NewGuid().ToString();
            var harvester = new Peeralize.Service.Harvester();
            type.UserId = userId;
            type.SaveType(userApiId);

            var saver = new MongoSink(userId);
            var modifier = new EntityFeatureGenerator(userId);
            modifier.LinkTo(saver, null); //We modify the entity to fill all it's data, then generate feature, and then save
            harvester.SetDestination(modifier);
            harvester.AddType(type, fileSource);
            harvester.Synchronize();
        }

        [Theory]
        [InlineData(new object[] {"TestData\\Ebag\\1156"})] //was 6
        public async void ParseEntityDump(string inputDirectory)
        {
            inputDirectory = Path.Combine(Environment.CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter()
            {
                Delimiter = ';'
            });
            var type = fileSource.GetTypeDefinition() as IntegrationTypeDefinition;
            Assert.NotNull(type);

            var userId = "123123123";
            var userApiId = Guid.NewGuid().ToString();
            var harvester = new Peeralize.Service.Harvester(20);
            type.UserId = userId;
            IntegrationTypeDefinition existingDataType;
            if (!IntegrationTypeDefinition.TypeExists(type, userId, out existingDataType))
            {
                type.SaveType(userApiId);
            }
            else type = existingDataType;
            var grouper = new GroupingBlock(userId,
                (document) => $"{document.GetString("uuid")}_{document.GetDate("ondate")?.Day}",
                (document) => document.Define("noticed_date", document.GetDate("ondate")).RemoveAll("event_id", "ondate", "value", "type"),
                AccumulateUserEvent); 
            //var saver = new MongoSink(userId); 
            var helper = new CrossSiteAnalyticsHelper(grouper.EntityDictionary, grouper.PageStats);
            grouper.LinkTo(DataflowBlock.NullTarget<IntegratedDocument>());
            grouper.Helper = helper;
            //demographyImporter.LinkTo(featureGen); 
            //featureGen.LinkTo(saver);

            //Group the users
            grouper.LinkOnComplete(new IntegrationActionBlock(userId, (block, doc) =>
            {
                //DumpUsergroupSessionsToCsv(grpr);
                DumpUsergroupSessionsToMongo(userId, block);
            }));

            harvester.SetDestination(grouper);
            harvester.AddType(type, fileSource);
            await harvester.Synchronize();
            var syncDuration = harvester.ElapsedTime();
            Debug.WriteLine($"Read all files in: {syncDuration.TotalSeconds}:{syncDuration.Milliseconds}");

            //Task.WaitAll(grouper.Completion, featureGen.Completion, );
            //await grouper.Completion;
            //Console.ReadLine(); // TODO: Fix dataflow action after grouping of all users
        }



        

        [Theory]
        [InlineData(new object[]{"TestData\\Ebag\\1156", "TestData\\Ebag\\demograpy.csv" })]
        public async void ExtractEntityFromDirectory(string inputDirectory, string demographySheet)
        {
            inputDirectory = Path.Combine(Environment.CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter());

            var type = fileSource.GetTypeDefinition() as IntegrationTypeDefinition;
            Assert.NotNull(type);

            var userId = "123123123";
            var userApiId = Guid.NewGuid().ToString();
            var harvester = new Peeralize.Service.Harvester();
            type.UserId = userId;
            IntegrationTypeDefinition existingDataType;
            if (!IntegrationTypeDefinition.TypeExists(type, userId, out existingDataType))
            {
                type.SaveType(userApiId);
            }
            else type = existingDataType;

            var grouper = new GroupingBlock(userId, GroupDocuments, FilterUserCreatedData, AccumulateUserEvent);
            var saver = new MongoSink(userId);
            var demographyImporter = new EntityDataImporter(demographySheet, true);
            //demographyImporter.SetEntityRelation((input, x) => input[0] == x.Document["uuid"]);
            demographyImporter.SetDataKey((input) => input[0] );
            demographyImporter.SetEntityKey((IntegratedDocument input) => input.GetDocument()?["uuid"].ToString());
            demographyImporter.JoinOn(JoinDemography);
            demographyImporter.Map();

            var helper = new CrossSiteAnalyticsHelper(grouper.EntityDictionary, grouper.PageStats); 

            demographyImporter.Helper = helper;
            grouper.Helper = helper;
            
            grouper.LinkTo(DataflowBlock.NullTarget<IntegratedDocument>());
            demographyImporter.LinkTo(DataflowBlock.NullTarget<IntegratedDocument>());
            var featureGen = new EntityFeatureGenerator(userId);
            featureGen.Helper = helper;
            //demographyImporter.LinkTo(featureGen); 
            featureGen.LinkTo(saver, null);

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
            harvester.AddType(type, fileSource);
            harvester.Synchronize();
            
            //Task.WaitAll(grouper.Completion, featureGen.Completion, );
            await saver.ProcessingCompletion;
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

        private object GroupDocuments(IntegratedDocument arg)
        {
            var argDocument = arg.GetDocument();
            var uuid = argDocument["uuid"].ToString();
            var date = DateTime.Parse(argDocument["ondate"].ToString());
            return $"{uuid}_{date.Day}"; //_{date.Day}";
        }

        private void DumpUsergroupSessionsToMongo(string userAppId, IntegrationBlock usersData)
        {
            var grouper = usersData as GroupingBlock;
            var typeDef = IntegrationTypeDefinition.CreateFromType<DomainUserSessionCollection>(userAppId);
            typeDef.AddField("is_paying", typeof(int));

            IntegrationTypeDefinition existingTypeDef;
            if (!IntegrationTypeDefinition.TypeExists(typeDef, userAppId, out existingTypeDef))
            {
                typeDef.Save();
            }
            else typeDef = existingTypeDef;

            try
            {
                foreach (var userDayInfoPairs in grouper.EntityDictionary)
                {
                    try
                    {
                        var user = grouper.EntityDictionary[userDayInfoPairs.Key];
                        var userDocument = user.GetDocument();
                        var userIsPaying = userDocument.Contains("is_paying") &&
                                           userDocument["is_paying"].AsInt32 == 1;
                        //We're only interested in paying users
                        //if (userIsPaying) continue;

                        var uuid = userDocument["uuid"].ToString(); 
                        var dateNoticed = DateTime.Parse(userDocument["noticed_date"].ToString());
                        userDocument["events"] =
                            ((BsonArray)userDocument["events"])
                            .OrderBy(x => DateTime.Parse(x["ondate"].ToString()))
                            .ToBsonArray();
                        var sessions = grouper.Helper.GetWebSessions(user).ToList();
                        var sessionWrapper = new DomainUserSessionCollection(sessions);
                        sessionWrapper.UserId = uuid;
                        sessionWrapper.Created = dateNoticed;

                        var document = IntegratedDocument.FromType(sessionWrapper, typeDef, userAppId);
                        var documentBson = document.GetDocument();
                        documentBson["is_paying"] = userIsPaying ? 1 : 0;
                        document.TypeId = typeDef.Id.Value;
                        //document.Save();
                    }
                    catch (Exception ex2)
                    { 
                    }
                }
            }
            catch (Exception ex)
            { 
            } 

        }
        private void DumpUsergroupSessionsToCsv(GroupingBlock usersData)
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
                        var user = usersData.EntityDictionary[userDayInfoPairs.Key];
                        var userDocument = user.GetDocument();
                        var userIsPaying = userDocument.Contains("is_paying") &&
                                           userDocument["is_paying"].AsInt32 == 1;
                        //We're only interested in paying users
                        if (!userIsPaying) continue;
                        var uuid = userDocument["uuid"].ToString();
                        userDocument["events"] =
                            ((BsonArray) userDocument["events"])
                            .OrderBy(x => DateTime.Parse(x["ondate"].ToString()))
                            .ToBsonArray(); 
                        foreach (DomainUserSession visitSession in usersData.Helper.GetWebSessions(user))
                        {
                            var newLine = string.Format("{0},{1},{2},{3}", uuid, visitSession.Domain,
                                visitSession.Duration.TotalSeconds, visitSession.Visited);
                            csvWr.WriteLine(newLine);
                        }
                    }
                    catch (Exception ex2)
                    {
                        ex2 = ex2;
                    }
                }
            }
            catch (Exception ex)
            {
                ex = ex;   
            }
            outputFs.Close();
        }

        /// <summary>
        /// Invoked after users are finished being grouped
        /// </summary>
        /// <param name="dataImporter"></param>
        /// <param name="usersData"></param>
        private void OnUsersGrouped(EntityDataImporter dataImporter, IntegrationBlock usersData)
        {
            var grouper = usersData as GroupingBlock;
            var userValues = grouper.EntityDictionary.Values; 
            //var today = DateTime.Today;
            var dateHelper = new DateHelper();
            double max_time_spent_by_any_paying_user_ebag =
                dataImporter.Helper.GetLongestVisitPurchaseDuration("ebag.bg", "payments/finish");
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            Calendar cal = dfi.Calendar;

            var purchasesCount = (double)_purchases.Count;
            if (purchasesCount == 0) purchasesCount = 1;
            var prob_buy_is_holiday = (double)_purchasesOnHolidays.Count / purchasesCount;
            var prob_buy_is_before_holiday = (double)_purchasesBeforeHolidays.Count / purchasesCount;
            var prop_buy_is_weekend = (double)_purchasesInWeekends.Count / purchasesCount;
            var mongoClient = typeof(IntegratedDocument).GetDataSource<IntegratedDocument>().MongoDb();
            int cnt = 0;
            int total = grouper.EntityDictionary.Count;
            foreach (var userDayInfoPairs in grouper.EntityDictionary)
            {
                var user = grouper.EntityDictionary[userDayInfoPairs.Key];
                var userDocument = user.GetDocument();
                userDocument["events"] =
                    ((BsonArray)userDocument["events"])
                    .OrderBy(x => DateTime.Parse(x["ondate"].ToString()))
                    .ToBsonArray();
                var uuid = userDocument["uuid"]; 
                var noticed = userDocument["noticed_date"].ToUniversalTime().ToString();
                var g_timestamp = userDocument["noticed_date"].ToUniversalTime();
                var weekstart = g_timestamp.StartOfWeek(DayOfWeek.Monday);
                g_timestamp = weekstart;
                var g_timestr = g_timestamp.ToString();
                userDocument["g_timestamp"] = g_timestamp;

                //var args = new FindAndModifyArgs();
                //args.Query = Query.EQ("Document.uuid", uuid);
                //args.Update = Update.Set("Document.g_timestamp", g_timestamp);
                //mongoClient.FindAndModify(args);


                userDocument["max_time_spent_by_any_paying_user_ebag"] = max_time_spent_by_any_paying_user_ebag; 

                userDocument["prob_buy_is_holiday"] = prob_buy_is_holiday;
                userDocument["prob_buy_is_before_holiday"] = prob_buy_is_before_holiday;
                userDocument["prop_buy_is_weekend"] = prop_buy_is_weekend;
                cnt++;
            }
            
            dataImporter.PostAll(userValues);
        }

        private void OnUserDemographyImported(EntityFeatureGenerator gen, GroupingBlock usersData)
        { 
            var userValues = usersData.EntityDictionary.Values;
            gen.Helper.GroupDemographics();
            gen.PostAll(userValues);
        }

        /// <summary>
        /// Format the initial user data
        /// </summary>
        /// <param name="obj"></param>
        private void FilterUserCreatedData(IntegratedDocument obj)
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
        private object AccumulateUserEvent(IntegratedDocument accumulator, IntegratedDocument newEntry)
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
            if (value.Contains("payments/finish") && value.ToHostname().Contains("ebag.bg"))
            {
                var dateTime = DateTime.Parse(onDate);
                if (_dateHelper.IsHoliday(dateTime))
                {
                    _purchasesOnHolidays.Add(newElement);
                }
                else if (_dateHelper.IsHoliday(dateTime.AddDays(1)))
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
