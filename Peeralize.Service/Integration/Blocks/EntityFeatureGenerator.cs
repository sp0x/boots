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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="intDoc"></param>
        /// <returns></returns>
        protected override IntegratedDocument OnBlockReceived(IntegratedDocument intDoc)
        {
            //TODO: Clean this up..
            var doc = intDoc.Document;
            var targetDomain = "ebag.bg";
            var userId = intDoc.Document["uuid"].AsString;
            var browsingStats = UserBrowsingStats.FromBson(doc["browsing_statistics"]);

            doc["events"] = ((BsonArray) doc["events"]);

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
            var usersWithSameAge = Helper.GetUsersWithSameAge(userAge);
            var buyersWithSameAge = Helper.GetBuyersWithSameAge(userAge);
            var usersWithSameAgeAndGender = Helper.GetUsersWithSameAgeAndGender(userAge, userGender);
            var buyersWithSameGender = Helper.GetBuyersWithSameGender(userGender);
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
                    if (dateTime >= lastWeekStart &&  dateTime <= lastWeekEnd) boughtLastWeek.Add(ebagVisit);
                    else if (dateTime >= lastMonthStart && dateTime <= lastMonthEnd) boughtLastMonth.Add(ebagVisit);
                    else if (dateTime >= lastYearStart && dateTime <= lastYearEnd)boughtLastYear.Add(ebagVisit);
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
            var probVisitAgeAndGender = usersWithSameAgeAndGender.Count() / Helper.GetDomainVisits("ebag.bg");
            var probBuyVisit = buysVisitsQ * probVisitAgeAndGender;

            Helper.AddBuyVisitValue(probBuyVisit);


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

            intDoc.Document["is_from_mobile"] = hasVisitedMobile ? 1 : 0;
            
            intDoc.Document["is_on_promotions_page"] = hasVisitedPromotion ? 1 : 0;
            intDoc.Document["before_visit_from_mobile"] = hasVisitedMobileBeforeTarget ? 1 : 0;
            intDoc.Document["time_before_leaving"] = browsingStats != null
                ? browsingStats.TargetSiteVisitAverageDuration
                : 0;
            var avgPageRating = Helper.GetAveragePageRating(domainVisits, targetDomain);
            intDoc.Document["page_rank"] = avgPageRating; 

            intDoc.Document["prop_buy_is_before_weekend_user"] = purchasesBeforeWeekends.Count() /
                                                                 mx1(purchases.Count);
            intDoc.Document["visits_before_weekend"] = visitsBeforeWeekends.Count / mx1(completeTimeSpan.Days);
            intDoc.Document["visits_before_holidays"] = visitsBeforeHolidays.Count / mx1(completeTimeSpan.Days);
            
            intDoc.Document["days_visited_ebag"] = daysVisitedEbag / mx1(completeTimeSpan.Days);
            intDoc.Document["mobile_visits"] = mobileEbagVisits.Count() / mx1(ebagVisits.Count());
            intDoc.Document["mobile_purchases"] = mobilePurchases.Count() / mx1(purchases.Count());


            //new features
            intDoc.Document["visited_ebag"] = (ebagVisits.Count > 0) ? 1 : 0;
            intDoc.Document["time_spent_online"] = realisticUserWebTime.TotalSeconds / 86400 * 7;
            intDoc.Document["time_spent_on_mobile_sites"] = browsingStats?.TimeOnMobileSites / mx1(realisticUserWebTime.TotalSeconds) ;
            intDoc.Document["time_spent_ebag"] = browsingStats?.TargetSiteTime / mx1(realisticUserWebTime.TotalSeconds);
            intDoc.Document["visits_on_holidays"] = visitsOnHolidays.Count / mx1(completeTimeSpan.Days);
            intDoc.Document["visits_on_weekends"] = visitsOnWeekends.Count / mx1(completeTimeSpan.Days);
            intDoc.Document["p_online_weekend"] = browsingStats.WeekendVisits / mx1(browsingStats.DomainChanges);
            intDoc.Document["p_buy_age_group"] = buyersWithSameAge.Count() / mxi1(usersWithSameAge.Count());
            intDoc.Document["p_buy_gender_group"] = buyersWithSameGender.Count() / mx1(usersWithSameGender.Count());

            intDoc.Document["p_visit_ebag_age"] = Helper.TargetDomainVisitors(usersWithSameAge).Count() / Helper.GetEntityCount();
            //p(visit_ebag|age)=visitors_same_age/visitors_total
            intDoc.Document["p_visit_ebag_gender"] = Helper.TargetDomainVisitors(usersWithSameGender).Count() / Helper.GetEntityCount();
            intDoc.Document["p_to_go_online"] = browsingStats.TargetSiteVisits / mx1(Helper.GetAverageDomainVisits());
            var highPagerankSites = Helper.GetHighrankingPages(targetDomain, 5).ToArray();
            intDoc.Document["avg_time_spent_on_high_pageranksites"] =
                highPagerankSites.Sum(x=> x.GetUserVisitDuration(userId)) / mx1(realisticUserWebTime.TotalSeconds);
            for (int iHighPage = 0; iHighPage < 5; iHighPage++)
            {
                if (iHighPage >= highPagerankSites.Length) continue;
                var name = $"highranking_page_{iHighPage}";
                intDoc.Document[name] = 0;
                var highPagerankSite = highPagerankSites[iHighPage];
                var targetRating = highPagerankSite.GetTargetRating(userId);
                intDoc.Document[name] = targetRating.Value;
               
            }

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