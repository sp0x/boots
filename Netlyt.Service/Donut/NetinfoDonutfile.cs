﻿using System;
using MongoDB.Bson;
using nvoid.db.Caching;
using nvoid.db.DB;
using nvoid.extensions;
using Netlyt.Service.Integration;
using Netlyt.Service.Models;
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
            foreach (var raw_event in events)
            {
                var evnt = raw_event as BsonDocument;
                var value = evnt.GetString("value");
                var onDate = evnt.GetDate("ondate").Value;
                var uuid = evnt.GetString("uuid");
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
                var isNewPage = false;
                if (!Context.PageStats.ContainsKey(pageSelector))
                {
                    Context.PageStats.TryAdd(pageSelector, new PageStats()
                    {
                        Page = value
                    });
                }
                Context.PageStats[pageSelector].PageVisitsTotal++;
                if (value.Contains("payments/finish") && value.ToHostname().Contains("ebag.bg"))
                {
                    if (DateHelper.IsHoliday(onDate))
                    {
                        Context.PurchasesOnHolidays.Add(uuid);
                    }
                    else if (DateHelper.IsHoliday(onDate.AddDays(1)))
                    {
                        Context.PurchasesBeforeHolidays.Add(uuid);
                    }
                    else if (onDate.DayOfWeek == DayOfWeek.Friday)
                    {
                        Context.PurchasesBeforeWeekends.Add(uuid);
                    }
                    else if (onDate.DayOfWeek > DayOfWeek.Friday)
                    {
                        Context.PurchasesInWeekends.Add(uuid);
                    }
                    Context.Purchases.Add(uuid);
                    Context.PayingUsers.Add(uuid);//["is_paying"] = 1;
                } 
            }
            Context.Cache();
        }


    }
}