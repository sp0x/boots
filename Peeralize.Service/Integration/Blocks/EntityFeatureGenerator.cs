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
            var browsingStats = UserBrowsingStats.FromBson(doc["browsing_statistics"]);
            doc["events"] = ((BsonArray) doc["events"]);

            Func<double, double> mx1 = (x) => Math.Max(1, x);
            var events = (BsonArray)doc["events"];
            IEnumerable<IGrouping<string, BsonValue>> domainVisits = events
                .GroupBy(x => x["value"].ToString().ToHostname(true));

            var ebagVisits = domainVisits?.Where(x => x.Key.ToString().Contains("ebag.bg"))
                .SelectMany(x => x.ToList()).ToBsonArray();

            var completeTimeSpan = CrossSiteAnalyticsHelper.GetPeriod(events);
            var realisticUserWebTime = CrossSiteAnalyticsHelper.GetDailyPeriodSum(events);
            var today = DateTime.Today;
            var visitsPerTime = events.Count / mx1(completeTimeSpan.Days);
            var lastWeekStart = today.AddDays(-(int)today.DayOfWeek - 6);
            var lastWeekEnd = lastWeekStart.AddDays(6);
            var dateHelper = new DateHelper();

            var currentMonthStart = new DateTime(today.Year, today.Month, 1);
            var lastMonthStart = currentMonthStart.AddMonths(-1);
            var lastMonthEnd = currentMonthStart.AddDays(-1);

            var lastYearStart = new DateTime(today.Year, 1, 1).AddYears(-1);
            var lastYearEnd = new DateTime(today.Year, 1, 1).AddDays(-1);
            DateTime lastMEbagVisit = DateTime.MinValue;
            var hasVisitedMobileBeforeTarget = false;
            var hasVisitedMobile = false;

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
                var hostname = visitUrl.ToHostname(true);
                var dateTime = DateTime.Parse(x["ondate"].ToString());
                if (hostname.Contains("m.ebag.bg"))
                {
                    hasVisitedMobile = true;
                    lastMEbagVisit = dateTime;
                }
                if (hostname == "ebag.bg")
                {
                    if (hasVisitedMobile && dateTime.Day == lastMEbagVisit.Day)
                    {
                        hasVisitedMobileBeforeTarget = true;
                    }
                }


                if (visitUrl.Contains("payments/finish"))
                {
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
            intDoc.Document["bought_last_week"] = boughtLastWeek.Count() /
                mx1(CrossSiteAnalyticsHelper.GetPeriod(boughtLastWeek).Days);
            intDoc.Document["bought_last_month"] = boughtLastMonth.Count() / 
                mx1(CrossSiteAnalyticsHelper.GetPeriod(boughtLastMonth).Days);
            intDoc.Document["bought_last_year"] = boughtLastYear.Count() / 
                mx1(CrossSiteAnalyticsHelper.GetPeriod(boughtLastYear).Days);
            intDoc.Document["time_spent"] = CrossSiteAnalyticsHelper.GetVisitsTimeSpan(ebagVisits, realisticUserWebTime).TotalSeconds;

            intDoc.Document["time_spent_max"] = domainVisits?.Select(x => 
                CrossSiteAnalyticsHelper.GetVisitsTimeSpan(x.ToBsonArray(), realisticUserWebTime)).Max().TotalSeconds;
            intDoc.Document["month"] = purchasesInThisMonth.Count() / 
                mx1(purchases.Count);
            intDoc.Document["prob_buy_is_holiday_user"] = purchasesInHolidays.Count() / 
                mx1(purchases.Count);
            intDoc.Document["prob_buy_is_before_holiday_user"] = purchasesBeforeHolidays.Count() / 
                mx1(purchases.Count);
            intDoc.Document["prop_buy_is_weekend_user"] = purchasesInWeekends.Count() /
                mx1(purchases.Count);
            intDoc.Document["before_visit_from_mobile"] = hasVisitedMobileBeforeTarget ? 1 : 0;
            intDoc.Document["time_before_leaving"] = browsingStats != null
                ? browsingStats.TargetSiteVisitAverageDuration
                : 0;
            //Cleanup events
            intDoc.Document.Remove("events");
            intDoc.Document.Remove("browsing_statistics");
//            //intDoc.Document["max_time_spent_by_any_paying_user_ebag"] =  
//            var averagePageRank = Helper.GetAveragePageRating(domainVisits, "ebag.bg");

            //TODO: Generate features
            return intDoc;
        }


    }
}