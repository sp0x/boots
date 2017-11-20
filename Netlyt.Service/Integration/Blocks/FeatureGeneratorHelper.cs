using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using MongoDB.Bson;
using nvoid.extensions;
using Netlyt.Service.Time;

namespace Netlyt.Service.Integration.Blocks
{
    /// <summary>
    /// 
    /// </summary>
    public class FeatureGeneratorHelper
    {
        public CrossSiteAnalyticsHelper Helper { get; set; }
        public string TargetDomain { get; set; }
        public FeatureGeneratorHelper() 
        { 
        }
         

        /// <summary>
        /// 
        /// </summary>
        /// <param name="intDoc"></param>
        /// <returns></returns>
        public TransformBlock<IntegratedDocument, IEnumerable<KeyValuePair<string, object>>> GetBlock()
        {
            var block = new TransformBlock<IntegratedDocument, IEnumerable<KeyValuePair<string, object>>>((doc) =>
            {
                return GetFeatures(doc);
            });
            return block;
        }


        public IEnumerable<KeyValuePair<string, object>> GetFeatures(IntegratedDocument intDoc)
        {
            //TODO: Clean this up..
            BsonDocument intDocDocument = intDoc.GetDocument();
            var doc = intDocDocument;
            var targetDomain = "ebag.bg";
            var userId = intDocDocument["uuid"].AsString;
            

            var purchasesCount = (double)Helper.Purchases.Count;
            double max_time_spent_by_any_paying_user_ebag = Helper.GetLongestVisitPurchaseDuration("ebag.bg", "payments/finish");
            //Todo: move this to the demo. importer...
            Helper.GroupDemographics();

            var browsingStats = UserBrowsingStats.FromBson(doc["browsing_statistics"]);
            var prob_buy_is_holiday = (double)Helper.PurchasesOnHolidays.Count / purchasesCount;
            var prob_buy_is_before_holiday = (double)Helper.PurchasesBeforeHolidays.Count / purchasesCount;
            var prop_buy_is_weekend = (double)Helper.PurchasesInWeekends.Count / purchasesCount;

            DateTime g_timestamp = doc["noticed_date"].AsDateTime.StartOfWeek(DayOfWeek.Monday); 
            yield return new KeyValuePair<string, object>("g_timestamp", g_timestamp);
            yield return new KeyValuePair<string, object>("is_paying",
                intDoc.Has("is_paying") && intDoc.GetInt("is_paying") == 1 ? 1 : 0);
            yield return new KeyValuePair<string, object>("max_time_spent_by_any_paying_user_ebag", max_time_spent_by_any_paying_user_ebag);
            yield return new KeyValuePair<string, object>("prob_buy_is_holiday", prob_buy_is_holiday);
            yield return new KeyValuePair<string, object>("prob_buy_is_before_holiday", prob_buy_is_before_holiday);
            yield return new KeyValuePair<string, object>("prop_buy_is_weekend", prop_buy_is_weekend);

            doc["events"] = ((BsonArray)doc["events"]);

            Func<double, double> mx1 = (x) => Math.Max(1, x);
            Func<int, double> mxi1 = (x) => Math.Max(1, x);
            var events = (BsonArray)doc["events"];
            IEnumerable<IGrouping<string, BsonValue>> domainVisits = events
                .GroupBy(x => x["value"].ToString().ToHostname(true));
            
            var ebagVisits = domainVisits?
                .Where(x => x.Key.ToString().Contains(targetDomain))
                .SelectMany(x => x.ToList()).ToBsonArray();

            var mobileEbagVisits = domainVisits?
                .Where(x => x.Key.ToString().ToHostname().StartsWith("m.ebag.bg"))
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
            var hasVisitedPromotion = false;
            int daysVisitedEbag = 0;
            DateTime? lastEbagVisit = null;
            int userAge = doc.Contains("age") ? doc["age"].AsInt32 : 0;
            string userGender = doc.Contains("gender") ? doc["gender"].ToString() : "0";
            var usersWithSameAge = Helper.GetUsersWithSameAge(userAge).Count();
            var buyersWithSameAge = Helper.GetBuyersWithSameAge(userAge);
            var usersWithSameAgeAndGender = Helper.GetUsersWithSameAgeAndGender(userAge, userGender);
            var buyersWithSameGender = Helper.GetBuyersWithSameGender(userGender).Count();
            var usersWithSameGender = Helper.GetusersWithSameGender(userGender);

            BsonArray boughtLastWeek = new BsonArray(),
                boughtLastMonth = new BsonArray(),
                boughtLastYear = new BsonArray(),
                purchasesInThisMonth = new BsonArray(),
                purchasesInHolidays = new BsonArray(),
                purchasesBeforeHolidays = new BsonArray(),
                purchasesInWeekends = new BsonArray(),
                mobilePurchases = new BsonArray(),
                purchasesBeforeWeekends = new BsonArray(),
                visitsOnWeekends = new BsonArray(),
                visitsOnHolidays = new BsonArray(),
                visitsBeforeWeekends = new BsonArray(),
                visitsBeforeHolidays = new BsonArray();


            List<BsonValue> purchases = new List<BsonValue>();
            foreach (var ebagVisit in ebagVisits)
            {
                var visitUrl = ebagVisit["value"].ToString();
                var hostname = visitUrl.ToHostname(true);
                var dateTime = DateTime.Parse(ebagVisit["ondate"].ToString());

                if (hostname.Contains("m.ebag.bg"))
                {
                    hasVisitedMobile = true;
                    lastMEbagVisit = dateTime;
                }
                if (hostname == targetDomain)
                {
                    if (hasVisitedMobile && dateTime.Day == lastMEbagVisit.Day)
                    {
                        hasVisitedMobileBeforeTarget = true;
                    }
                }
                //Distinct all days
                if (lastEbagVisit == null || lastEbagVisit.Value.DayOfYear != dateTime.DayOfYear)
                {
                    daysVisitedEbag++;
                    var dateTimeNextDay = dateTime.AddDays(1);
                    if (dateTime.DayOfWeek == DayOfWeek.Friday) visitsBeforeWeekends.Add(ebagVisit);
                    else if (dateTime.DayOfWeek > DayOfWeek.Friday) visitsOnWeekends.Add(ebagVisit);
                    else if (dateHelper.IsHoliday(dateTimeNextDay)) visitsBeforeHolidays.Add(ebagVisit);
                    else if (dateHelper.IsHoliday(dateTime)) visitsOnHolidays.Add(ebagVisit);
                }

                if (!hasVisitedPromotion && visitUrl.Contains("/promo-products"))
                {
                    hasVisitedPromotion = true;
                }
                if (visitUrl.Contains("payments/finish"))
                {
                    var dateTimeNextDay = dateTime.AddDays(1);
                    if (dateTime >= lastWeekStart && dateTime <= lastWeekEnd) boughtLastWeek.Add(ebagVisit);
                    else if (dateTime >= lastMonthStart && dateTime <= lastMonthEnd) boughtLastMonth.Add(ebagVisit);
                    else if (dateTime >= lastYearStart && dateTime <= lastYearEnd) boughtLastYear.Add(ebagVisit);
                    else if (dateTime.Month == today.Month) purchasesInThisMonth.Add(ebagVisit);
                    else if (dateHelper.IsHoliday(dateTime)) purchasesInHolidays.Add(ebagVisit);
                    else if (dateHelper.IsHoliday(dateTimeNextDay)) purchasesBeforeHolidays.Add(ebagVisit);
                    else if (dateTime.DayOfWeek > DayOfWeek.Friday) purchasesInWeekends.Add(ebagVisit);
                    else if (dateTime.DayOfWeek == DayOfWeek.Friday) purchasesBeforeWeekends.Add(ebagVisit);
                    purchases.Add(ebagVisit);
                    if (hostname.Contains("m.ebag.bg"))
                    {
                        mobilePurchases.Add(ebagVisit);
                    }
                }
                lastEbagVisit = dateTime;
            }
            var buysVisitsQ = purchases.Count / mx1(browsingStats.TargetSiteVisits);
            var sameAgeAndGenderUsers = usersWithSameAgeAndGender.Count();
            var targetVisits = Helper.GetDomainVisitors("ebag.bg");
            var probVisitAgeAndGender = sameAgeAndGenderUsers / targetVisits;
            var probBuyVisit = buysVisitsQ * probVisitAgeAndGender;

            Helper.AddBuyVisitValue(probBuyVisit);
            Func<string, object, KeyValuePair<string, object>> toPair = (x, y) => new KeyValuePair<string, object>(x, y);

            yield return toPair("visits_per_time", visitsPerTime);
            yield return toPair("bought_last_week", boughtLastWeek.Count() / mx1(CrossSiteAnalyticsHelper.GetPeriod(boughtLastWeek).Days));
            yield return toPair("bought_last_month", boughtLastMonth.Count() /
                                                                               mx1(CrossSiteAnalyticsHelper.GetPeriod(boughtLastMonth).Days));
            yield return toPair("bought_last_year", boughtLastYear.Count() /
                                                                              mx1(CrossSiteAnalyticsHelper.GetPeriod(boughtLastYear).Days));
            yield return toPair("time_spent", CrossSiteAnalyticsHelper.GetVisitsTimeSpan(ebagVisits, realisticUserWebTime).TotalSeconds);
            yield return toPair("time_spent_max", domainVisits?.Select(x =>
                CrossSiteAnalyticsHelper.GetVisitsTimeSpan(x.ToBsonArray(), realisticUserWebTime)).Max().TotalSeconds);
            yield return toPair("month", purchasesInThisMonth.Count() /
                                                                   mx1(purchases.Count));
            yield return toPair("prob_buy_is_holiday_user", purchasesInHolidays.Count() /
                                                                                      mx1(purchases.Count));
            yield return toPair("prob_buy_is_before_holiday_user", purchasesBeforeHolidays.Count() /
                                                                                             mx1(purchases.Count));
            yield return toPair("prop_buy_is_weekend_user", purchasesInWeekends.Count() /
                                                                                      mx1(purchases.Count));
            yield return toPair("is_from_mobile", hasVisitedMobile ? 1 : 0);
            yield return toPair("is_on_promotions_page", hasVisitedPromotion ? 1 : 0);
            yield return toPair("before_visit_from_mobile", hasVisitedMobileBeforeTarget ? 1 : 0);
            yield return toPair("time_before_leaving", browsingStats != null
                ? browsingStats.TargetSiteVisitAverageDuration
                : 0);
            yield return toPair("page_rank",
                Helper.GetAveragePageRating(domainVisits, targetDomain, false));


            yield return toPair("prop_buy_is_before_weekend_user", purchasesBeforeWeekends.Count() /
                                                                   mx1(purchases.Count));
            yield return toPair("visits_before_weekend", visitsBeforeWeekends.Count / mx1(completeTimeSpan.Days));
            yield return toPair("visits_before_holidays", visitsBeforeHolidays.Count / mx1(completeTimeSpan.Days));

            yield return toPair("days_visited_ebag", daysVisitedEbag / mx1(completeTimeSpan.Days));
            yield return toPair("mobile_visits", mobileEbagVisits.Count() / mx1(ebagVisits.Count()));
            yield return toPair("mobile_purchases", mobilePurchases.Count() / mx1(purchases.Count()));

            var entityCount = Helper.GetEntityCount();

            yield return toPair("visited_ebag", (ebagVisits.Count > 0) ? 1 : 0);
            yield return toPair("time_spent_online", realisticUserWebTime.TotalSeconds / 86400 * 7);
            yield return toPair("time_spent_on_mobile_sites", browsingStats?.TimeOnMobileSites / mx1(realisticUserWebTime.TotalSeconds));
            yield return toPair("time_spent_ebag", browsingStats?.TargetSiteTime / mx1(realisticUserWebTime.TotalSeconds));
            yield return toPair("visits_on_holidays", visitsOnHolidays.Count / mx1(completeTimeSpan.Days));
            yield return toPair("visits_on_weekends", visitsOnWeekends.Count / mx1(completeTimeSpan.Days));
            yield return toPair("p_online_weekend", browsingStats.WeekendVisits / mx1(browsingStats.DomainChanges));
            yield return toPair("p_buy_age_group", buyersWithSameAge.Count() / mxi1(usersWithSameAge));
            yield return toPair("p_buy_gender_group", buyersWithSameGender / mx1(usersWithSameGender.Count()));

            yield return toPair("p_visit_ebag_age", usersWithSameAge / entityCount);
            //p(visit_ebag|age)=visitors_same_age/visitors_total
            yield return toPair("p_visit_ebag_gender", Helper.TargetDomainVisitors(usersWithSameGender).Count() / entityCount);
            yield return toPair("p_to_go_online", browsingStats.TargetSiteVisits / mx1(Helper.GetAverageDomainVisits()));
            yield return toPair("p_buy_visit", Helper.BuyVisitValues);
            var highPagerankSites = Helper.GetHighrankingPages(targetDomain, 5).ToArray();
            yield return toPair("avg_time_spent_on_high_pageranksites",
                highPagerankSites.Sum(x => x.GetUserVisitDuration(userId)) / mx1(realisticUserWebTime.TotalSeconds));

            for (int iHighPage = 0; iHighPage < 5; iHighPage++)
            {
                if (iHighPage >= highPagerankSites.Length) continue;
                var name = $"highranking_page_{iHighPage}";
                intDocDocument[name] = 0;
                var highPagerankSite = highPagerankSites[iHighPage];
                var targetRating = highPagerankSite.GetTargetRating(targetDomain); //userId
                var endValue = targetRating != null ? targetRating.Value : 0;
                yield return toPair(name, endValue);
            }
            //Cleanup events
            //intDocDocument.Remove("events");
            //intDocDocument.Remove("browsing_statistics");
            //            //intDoc.Document["max_time_spent_by_any_paying_user_ebag"] =  
            //            var averagePageRank = Helper.GetAveragePageRating(domainVisits, "ebag.bg"); 
        }


        public IEnumerable<KeyValuePair<string, object>> GetAvgTimeBetweenSessionFeatures(IntegratedDocument doc)
        {
            int min = 0, max = 604800;
            var sessions = CrossSiteAnalyticsHelper.GetWebSessions(doc, TargetDomain)
                .Select(x => x.Visited).ToList();
            var timeBetweenSessionSum = 0.0d;
            for (var i = 0; i < sessions.Count; i++)
            {
                if (i == 0) continue;
                var session = sessions[i];
                var diff = session - sessions[i - 1];
                timeBetweenSessionSum += diff.TotalSeconds;
            }
            timeBetweenSessionSum = timeBetweenSessionSum / Math.Max(sessions.Count, 1); 
            timeBetweenSessionSum = timeBetweenSessionSum == 0 ? 0 : (1 - (timeBetweenSessionSum / max));
            yield return new KeyValuePair<string, object>("time_between_visits_avg", timeBetweenSessionSum);
        }
    }
}