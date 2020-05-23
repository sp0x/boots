using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using MongoDB.Bson;
using nvoid.extensions;
using Netlyt.Interfaces;
using Netlyt.Service;
using Netlyt.Service.Integration; 
using Netlyt.Service.Time;

namespace Netlyt.ServiceTests.Netinfo
{
    /// <summary>
    /// Helps with premade feature generation blocks.
    /// </summary>
    public class NetinfoFeatureGeneratorHelper
    {
        public NetinfoDonutfile Donut { get; set; }
        public string TargetDomain { get; set; }
        public NetinfoFeatureGeneratorHelper()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="intDoc"></param>
        /// <returns></returns>
        public TransformBlock<IIntegratedDocument, IEnumerable<KeyValuePair<string, object>>> GetBlock()
        {
            var block = new TransformBlock<IIntegratedDocument, IEnumerable<KeyValuePair<string, object>>>((doc) =>
            {
                return GetFeatures(doc);
            });
            return block;
        }


        public IEnumerable<KeyValuePair<string, object>> GetFeatures(IIntegratedDocument intDoc)
        {
            //TODO: Clean this up..
            BsonDocument intDocDocument = intDoc.GetDocument();
            var doc = intDocDocument;
            var userId = intDocDocument["uuid"].AsString;
            var browsingStats = Donut.Context.UserBrowsingStats.GetOrAddHash(userId);
            
            var purchasesCount = (double)Donut.Context.Purchases.Count();
            double max_time_spent_by_any_paying_user_ebag = Donut.GetLongestWebSessionDuration();//(TargetDomain, "payments/finish");
            
            var prob_buy_is_holiday = (double)Donut.Context.PurchasesOnHolidays.Count() / purchasesCount;
            var prob_buy_is_before_holiday = (double)Donut.Context.PurchasesBeforeHolidays.Count() / purchasesCount;
            var prop_buy_is_weekend = (double)Donut.Context.PurchasesInWeekends.Count() / purchasesCount;

            DateTime g_timestamp = doc["noticed_date"].ToUniversalTime().StartOfWeek(DayOfWeek.Monday);
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
                .Where(x => x.Key.ToString().Contains(TargetDomain))
                .SelectMany(x => x.ToList()).ToBsonArray();

            var mobileEbagVisits = domainVisits?
                .Where(x => x.Key.ToString().ToHostname().StartsWith($"m.{TargetDomain}"))
                .SelectMany(x => x.ToList()).ToBsonArray();


            var completeTimeSpan = new TimeSpan();//CrossSiteAnalyticsHelper.GetPeriod(events);
            var realisticUserWebTime = new TimeSpan(); //CrossSiteAnalyticsHelper.GetDailyPeriodSum(events);
            var today = DateTime.Today;
            var visitsPerTime = events.Count / mx1(completeTimeSpan.Days);
            var lastWeekStart = today.AddDays(-(int)today.DayOfWeek - 6);
            var lastWeekEnd = lastWeekStart.AddDays(6);

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
            var usersWithSameAge = Donut.GetUsersWithSameAgeCount(userAge);
            var buyersWithSameAge = Donut.GetBuyersWithSameAgeCount(userAge);
            var usersWithSameAgeAndGender = Donut.GetUsersWithSameAgeAndGenderCount(userAge, userGender);
            var buyersWithSameGender = Donut.GetBuyersWithSameGenderCount(userGender);
            var usersWithSameGender = Donut.GetUsersWithSameGenderCount(userGender);

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

                if (hostname.Contains($"m.{TargetDomain}"))
                {
                    hasVisitedMobile = true;
                    lastMEbagVisit = dateTime;
                }
                if (hostname == TargetDomain)
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
                    else if (DateHelper.IsHoliday(dateTimeNextDay)) visitsBeforeHolidays.Add(ebagVisit);
                    else if (DateHelper.IsHoliday(dateTime)) visitsOnHolidays.Add(ebagVisit);
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
                    else if (DateHelper.IsHoliday(dateTime)) purchasesInHolidays.Add(ebagVisit);
                    else if (DateHelper.IsHoliday(dateTimeNextDay)) purchasesBeforeHolidays.Add(ebagVisit);
                    else if (dateTime.DayOfWeek > DayOfWeek.Friday) purchasesInWeekends.Add(ebagVisit);
                    else if (dateTime.DayOfWeek == DayOfWeek.Friday) purchasesBeforeWeekends.Add(ebagVisit);
                    purchases.Add(ebagVisit);
                    if (hostname.Contains($"m.{TargetDomain}"))
                    {
                        mobilePurchases.Add(ebagVisit);
                    }
                }
                lastEbagVisit = dateTime;
            }
            var buysVisitsQ = purchases.Count / mx1(browsingStats.TargetSiteVisits);
            var sameAgeAndGenderUsers = usersWithSameAgeAndGender;
            var targetVisits = Donut.GetDomainVisitorsCount(TargetDomain);
            var probVisitAgeAndGender = sameAgeAndGenderUsers / targetVisits;
            var probBuyVisit = buysVisitsQ * probVisitAgeAndGender;

            Donut.AddBuyVisitValue(probBuyVisit);
            Func<string, object, KeyValuePair<string, object>> toPair = (x, y) => new KeyValuePair<string, object>(x, y);

            yield return toPair("visits_per_time", visitsPerTime);
//            yield return toPair("bought_last_week", boughtLastWeek.Count() / mx1(CrossSiteAnalyticsHelper.GetPeriod(boughtLastWeek).Days));
//            yield return toPair("bought_last_month", boughtLastMonth.Count() /
//                                                                               mx1(CrossSiteAnalyticsHelper.GetPeriod(boughtLastMonth).Days));
//            yield return toPair("bought_last_year", boughtLastYear.Count() /
//                                                                              mx1(CrossSiteAnalyticsHelper.GetPeriod(boughtLastYear).Days));
//            yield return toPair("time_spent", CrossSiteAnalyticsHelper.GetVisitsTimeSpan(ebagVisits, realisticUserWebTime).TotalSeconds);
//            yield return toPair("time_spent_max", domainVisits?.Select(x =>
//                CrossSiteAnalyticsHelper.GetVisitsTimeSpan(x.ToBsonArray(), realisticUserWebTime)).Max().TotalSeconds);
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
                Donut.GetAveragePageRating(domainVisits, TargetDomain, false));


            yield return toPair("prop_buy_is_before_weekend_user", purchasesBeforeWeekends.Count() /
                                                                   mx1(purchases.Count));
            yield return toPair("visits_before_weekend", visitsBeforeWeekends.Count / mx1(completeTimeSpan.Days));
            yield return toPair("visits_before_holidays", visitsBeforeHolidays.Count / mx1(completeTimeSpan.Days));

            yield return toPair("days_visited_ebag", daysVisitedEbag / mx1(completeTimeSpan.Days));
            yield return toPair("mobile_visits", mobileEbagVisits.Count() / mx1(ebagVisits.Count()));
            yield return toPair("mobile_purchases", mobilePurchases.Count() / mx1(purchases.Count()));

            var entityCount = Donut.GetEntityCount();

            yield return toPair("visited_ebag", (ebagVisits.Count > 0) ? 1 : 0);
            yield return toPair("time_spent_online", realisticUserWebTime.TotalSeconds / 86400 * 7);
            yield return toPair("time_spent_on_mobile_sites", browsingStats?.TimeOnMobileSites / mx1(realisticUserWebTime.TotalSeconds));
            yield return toPair("time_spent_ebag", browsingStats?.TargetSiteTime / mx1(realisticUserWebTime.TotalSeconds));
            yield return toPair("visits_on_holidays", visitsOnHolidays.Count / mx1(completeTimeSpan.Days));
            yield return toPair("visits_on_weekends", visitsOnWeekends.Count / mx1(completeTimeSpan.Days));
            yield return toPair("p_online_weekend", browsingStats.WeekendVisits / mx1(browsingStats.DomainChanges));
            yield return toPair("p_buy_age_group", buyersWithSameAge / mxi1(usersWithSameAge));
            yield return toPair("p_buy_gender_group", buyersWithSameGender / mx1(usersWithSameGender));

            yield return toPair("p_visit_ebag_age", usersWithSameAge / entityCount);
            //p(visit_ebag|age)=visitors_same_age/visitors_total
            yield return toPair("p_visit_ebag_gender", Donut.TargetDomainVisitorsCount(usersWithSameGender) / entityCount);
            yield return toPair("p_to_go_online", browsingStats.TargetSiteVisits / mx1(Donut.GetAverageDomainVisits()));
            yield return toPair("p_buy_visit", Donut.GetBuyVisitValues());
            var highPagerankSites = Donut.GetHighrankingPages(TargetDomain, 5).ToArray();
            yield return toPair("avg_time_spent_on_high_pageranksites",
                highPagerankSites.Sum(x => x.GetUserVisitDuration(userId)) / mx1(realisticUserWebTime.TotalSeconds));

            for (int iHighPage = 0; iHighPage < 5; iHighPage++)
            {
                if (iHighPage >= highPagerankSites.Length) continue;
                var name = $"highranking_page_{iHighPage}";
                intDocDocument[name] = 0;
                var highPagerankSite = highPagerankSites[iHighPage];
                var targetRating = highPagerankSite.GetTargetRating(TargetDomain); //userId
                var endValue = targetRating != null ? targetRating.Value : 0;
                yield return toPair(name, endValue);
            }
            //Cleanup events
            //intDocDocument.Remove("events");
            //intDocDocument.Remove("browsing_statistics");
            //            //intDoc.Document["max_time_spent_by_any_paying_user_ebag"] =  
            //            var averagePageRank = Helper.GetAveragePageRating(domainVisits, "ebag.bg"); 
        }


        public IEnumerable<KeyValuePair<string, object>> GetAvgTimeBetweenSessionFeatures(IIntegratedDocument doc)
        {
            int max = 604800;
            var sessions = Donut.GetWebSessions(doc, TargetDomain)
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