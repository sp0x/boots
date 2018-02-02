using System;
using System.Diagnostics;
using MongoDB.Bson;
using nvoid.db.Caching;
using nvoid.db.DB;
using nvoid.extensions;
using Netlyt.Service.Integration;
using Netlyt.Service.Models;
using Netlyt.Service.Models.Netinfo;
using Netlyt.Service.Time;

namespace Netlyt.Service.Donut
{
    public class NetinfoDonutfile
        : Donutfile<NetinfoDonutContext>
    {  
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacher"></param>
        public NetinfoDonutfile(RedisCacher cacher) : base(cacher)
        { 
        }
        
        const int META_NUMERIC_TYPE_VALUE = 1;

        /// <summary>
        /// 
        /// </summary> 
        /// <param name="intDoc"></param>
        public void ProcessRecord(IntegratedDocument intDoc)
        {
            var entry = intDoc?.Document?.Value;
            if (entry == null) return;
            var events = entry["events"] as BsonArray;
            var uuid = entry["uuid"].ToString();
            //var userObj = Context.UserCookies.AddOrMerge(uuid, new NetinfoUserCookie {Uuid = uuid});
            foreach (var raw_event in events)
            {
                var evnt = raw_event as BsonDocument;
                var value = evnt.GetString("value");
                var onDate = evnt.GetDate("ondate").Value;
                var type = evnt.GetInt("type");
                if (value.IsNumeric())
                {
                    var metaVal = $"{type}_{value}";
                    if (string.IsNullOrEmpty(uuid)) return;
                    Context.IncrementMetaCategory(META_NUMERIC_TYPE_VALUE, metaVal);
                    Context.AddEntityMetaCategory(uuid, META_NUMERIC_TYPE_VALUE, metaVal);
                } 
                var pageHost = value.ToHostname();
                var pageSelector = pageHost;
                Context.PageStats.AddOrMerge(pageSelector, new PageStats()
                {
                    Page = value, PageVisitsTotal = 1
                }); 
                if (value.Contains("payments/finish") && pageHost.Contains("ebag.bg"))
                {
                    if (DateHelper.IsHoliday(onDate))Context.PurchasesOnHolidays.Add(uuid);
                    else if (DateHelper.IsHoliday(onDate.AddDays(1)))Context.PurchasesBeforeHolidays.Add(uuid);
                    else if (onDate.DayOfWeek == DayOfWeek.Friday)Context.PurchasesBeforeWeekends.Add(uuid);
                    else if (onDate.DayOfWeek > DayOfWeek.Friday)Context.PurchasesInWeekends.Add(uuid);
                    Context.Purchases.Add(uuid);
                    Context.PayingUsers.Add(uuid);//["is_paying"] = 1;
                } 
            }
            Context.CacheAndClear();
        }


    }
}