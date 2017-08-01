using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MongoDB.Bson;
using nvoid.extensions;
using Peeralize.Service;
using Peeralize.Service.Format;
using Peeralize.Service.Integration;
using Peeralize.Service.Integration.Blocks;
using Peeralize.Service.IntegrationSource;
using Peeralize.Service.Source;
using Peeralize.ServiceTests.IntegrationSource;
using Xunit;

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
            var harvester = new Harvester();
            type.UserId = userId;
            type.SaveType(userApiId);

            var saver = new MongoSink(userId);
            var modifier = new EntityFeatureGenerator(userId);
            modifier.LinkTo(saver); //We modify the entity to fill all it's data, then generate feature, and then save

            harvester.SetDestination(modifier);
            harvester.AddType(type, fileSource);
            harvester.Synchronize();
        }



        [Theory]
        [InlineData("TestData\\Ebag\\1156")]
        public async void ExtractEntityFromDirectory(string inputDirectory)
        {
            inputDirectory = Path.Combine(Environment.CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter());

            var type = fileSource.GetTypeDefinition() as IntegrationTypeDefinition;
            Assert.NotNull(type);

            var userId = "123123123";
            var userApiId = Guid.NewGuid().ToString();
            var harvester = new Harvester();
            type.UserId = userId;
            type.SaveType(userApiId);

            var grouper = new EntityGroup(userId, (x) => x.Document["uuid"], FilterUserData, AccumulateUserEvent);
            var saver = new MongoSink(userId);
            grouper.LinkTo(DataflowBlock.NullTarget<IntegratedDocument>());
            var featureGen = new EntityFeatureGenerator(userId);
            featureGen.Helper = new CrossSiteAnalyticsHelper(grouper.EntityDictionary, grouper.PageStats);
            featureGen.LinkTo(saver);
            saver.LinkTo(DataflowBlock.NullTarget<IntegratedDocument>());

            grouper.ContinueWith((userdata) =>
            {
                OnUsersGrouped(featureGen, userdata);
            }); //modifier.PostAll);
            
            harvester.SetDestination(grouper);
            harvester.AddType(type, fileSource);
            harvester.Synchronize();
            
            //Task.WaitAll(grouper.Completion, featureGen.Completion, );
            await saver.Completion;
        }

        /// <summary>
        /// Invoked after users are finished being grouped
        /// </summary>
        /// <param name="featureGen"></param>
        /// <param name="usersData"></param>
        private void OnUsersGrouped(EntityFeatureGenerator featureGen, Dictionary<object, IntegratedDocument>.ValueCollection usersData)
        {

            var today = DateTime.Today;
            var dateHelper = new DateHelper();
            double max_time_spent_by_any_paying_user_ebag =
                featureGen.Helper.GetLongestVisitPurchaseDuration("ebag.bg", "payments/finish");
            var day = (double)((int)today.DayOfWeek / (double)7);
            var date = (double)(today.Day / (new DateTime(today.Year, 12, 31).Subtract(new DateTime(today.Year - 1, 12, 31)).Days));
            var dayTimeSpan = DateTime.Now - today;
            var time_of_day = (double)dayTimeSpan.TotalSeconds / (double)86400;

            var is_holiday = dateHelper.IsHoliday(today);
            var is_weekend = (int)today.DayOfWeek >= 6;
            var is_before_weekend = today.DayOfWeek == DayOfWeek.Friday;
            var is_before_holiday = dateHelper.IsHoliday(today.AddDays(1));
            var purchasesCount = (double)_purchases.Count;
            if (purchasesCount == 0) purchasesCount = 1;
            var prob_buy_is_holiday = (double)_purchasesOnHolidays.Count / purchasesCount;
            var prob_buy_is_before_holiday = (double)_purchasesBeforeHolidays.Count / purchasesCount;
            var prop_buy_is_weekend = (double)_purchasesInWeekends.Count / purchasesCount;
            foreach (var user in usersData)
            {
                user.Document["day"] = day;
                user.Document["date"] = date;
                user.Document["max_time_spent_by_any_paying_user_ebag"] = max_time_spent_by_any_paying_user_ebag;
                user.Document["time_of_day"] = time_of_day;
                user.Document["is_holiday"] = is_holiday ? 1 : 0;
                user.Document["is_weekend"] = is_weekend ? 1 : 0;
                user.Document["is_before_weekend"] = is_before_weekend ? 1 : 0;
                user.Document["is_before_holiday"] = is_before_holiday ? 1 : 0;
                user.Document["prob_buy_is_holiday"] = prob_buy_is_holiday;
                user.Document["prob_buy_is_before_holiday"] = prob_buy_is_before_holiday;
                user.Document["prop_buy_is_weekend"] = prop_buy_is_weekend;
            }
            featureGen.PostAll(usersData);
        }

        /// <summary>
        /// Format the initial user data
        /// </summary>
        /// <param name="obj"></param>
        private void FilterUserData(IntegratedDocument obj)
        {
            obj.Document.Remove("event_id");
            obj.Document.Remove("ondate");
            obj.Document.Remove("value");
            obj.Document.Remove("type"); 
        }

        /// <summary>
        /// Format the input element that should be added
        /// </summary>
        /// <param name="accumulator"></param>
        /// <param name="newEntry"></param>
        private object AccumulateUserEvent(IntegratedDocument accumulator, IntegratedDocument newEntry)
        {
            var value = newEntry.GetString("value");
            var onDate = newEntry.GetString("ondate");
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
            }
            return newElement;
        }

        private void Grouper_GroupingComplete(string userId, Task obj)
        {
            var saver = new MongoSink(userId);
            throw new NotImplementedException();
        }
         
    }
}
