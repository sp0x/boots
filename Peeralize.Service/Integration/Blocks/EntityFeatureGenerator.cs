using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using nvoid.extensions;

namespace Peeralize.Service.Integration.Blocks
{
    /// <summary>
    /// 
    /// </summary>
    public class EntityFeatureGenerator : IntegrationBlock
    {
        public CrossSiteAnalyticsHelper Helper { get; set; }

        public EntityFeatureGenerator(string userId, int capacity = 1000 * 1000) : base(capacity)
        {
            base.UserId = userId;
        }


        protected override IntegratedDocument OnBlockReceived(IntegratedDocument intDoc)
        {
            //TODO: Clean this up..
            var doc = intDoc.Document;
            doc["events"] =
                new BsonArray(((BsonArray) doc["events"])
                    .Where(x => !x["value"].ToString().IsNumeric())
                    .OrderBy(v => DateTime.Parse(v["ondate"].ToString())));
            var events = (BsonArray)doc["events"];
            IEnumerable<IGrouping<string, BsonValue>> siteVisits = events.GroupBy(x => x["value"].ToString().ToHostname(true));

            var ebagVisits = siteVisits?.Where(x => x.Key.ToString().Contains("ebag.bg"))
                .SelectMany(x => x.ToList()).ToBsonArray();
            var completeTimeSpan = CrossSiteAnalyticsHelper.GetPeriod(events);
            var realisticUserWebTime = CrossSiteAnalyticsHelper.GetDailyPeriodSum(events);
            var today = DateTime.Today;
            var visitsPerTime = events.Count / completeTimeSpan.Days;
            var lastWeekStart = today.AddDays(-(int)today.DayOfWeek - 6);
            var lastWeekEnd = lastWeekStart.AddDays(7);
            var dateHelper = new DateHelper();

            var currentMonthStart = new DateTime(today.Year, today.Month, 1);
            var lastMonthStart = currentMonthStart.AddMonths(-1);
            var lastMonthEnd = currentMonthStart.AddDays(-1);

            var lastYearStart = new DateTime(today.Year, 1, 1).AddYears(-1);
            var lastYearEnd = new DateTime(today.Year, 1, 1).AddDays(-1);

            BsonArray boughtLastWeek = new BsonArray(),
                boughtLastMonth = new BsonArray(),
                boughtLastYear = new BsonArray(),
                purchasesInThisMonth = new BsonArray(),
                purchasesInHolidays = new BsonArray(),
                purchasesBeforeHolidays = new BsonArray(),
                purchasesInWeekends = new BsonArray();


            List<BsonValue> purchases = new List<BsonValue>();
            foreach (var x in ebagVisits)
            {
                var visitUrl = x["value"].ToString();
                if (visitUrl.Contains("payments/finish"))
                {
                    var dateTime = DateTime.Parse(x["ondate"].ToString());
                    var dateTimeNextDay = dateTime.AddDays(1);
                    if (dateTime >= lastWeekStart &&  dateTime <= lastWeekEnd) boughtLastWeek.Add(x);
                    else if (dateTime >= lastMonthStart && dateTime <= lastMonthEnd) boughtLastMonth.Add(x);
                    else if (dateTime >= lastYearStart && dateTime <= lastYearEnd)boughtLastYear.Add(x);
                    else if (dateTime.Month == today.Month) purchasesInThisMonth.Add(x);
                    else if (dateHelper.IsHoliday(dateTime)) purchasesInHolidays.Add(x);
                    else if (dateHelper.IsHoliday(dateTimeNextDay)) purchasesBeforeHolidays.Add(x);
                    else if (dateTime.DayOfWeek > DayOfWeek.Friday) purchasesInWeekends.Add(x);
                    purchases.Add(x);
                }
            } 

            intDoc.Document["visits_per_time"] = visitsPerTime;
            intDoc.Document["bought_last_week"] = boughtLastWeek.Count() / CrossSiteAnalyticsHelper.GetPeriod(boughtLastWeek).Days;
            intDoc.Document["bought_last_month"] = boughtLastMonth.Count() / CrossSiteAnalyticsHelper.GetPeriod(boughtLastMonth).Days;
            intDoc.Document["bought_last_year"] = boughtLastYear.Count() / CrossSiteAnalyticsHelper.GetPeriod(boughtLastYear).Days;
            intDoc.Document["time_spent"] = CrossSiteAnalyticsHelper.GetVisitsTimeSpan(ebagVisits, realisticUserWebTime).Seconds;
            intDoc.Document["time_spent_max"] = siteVisits?.Select(x => 
                CrossSiteAnalyticsHelper.GetVisitsTimeSpan(x.ToBsonArray(), realisticUserWebTime)).Max().Seconds;
            intDoc.Document["month"] = purchasesInThisMonth.Count() / purchases.Count;
            intDoc.Document["prob_buy_is_holiday_user"] = purchasesInHolidays.Count() / purchases.Count;
            intDoc.Document["prob_buy_is_before_holiday_user"] = purchasesBeforeHolidays.Count() / purchases.Count;
            intDoc.Document["prop_buy_is_weekend_user"] = purchasesInWeekends.Count() / purchases.Count;

            //intDoc.Document["max_time_spent_by_any_paying_user_ebag"] =  
            var averagePageRank = Helper.GetAveragePageRating(siteVisits, "ebag.bg");

            //TODO: Generate features
            return intDoc;
        }


    }
}