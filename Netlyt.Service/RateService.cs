using System.Collections.Generic;
using System.Linq;
using Donut.Orion;
using EntityFramework.DbContextScope.Interfaces;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Data;

namespace Netlyt.Service
{
    public class RateService : IRateService
    {
        private IRedisCacher _cacher;
        private IDbContextScopeFactory _dbContextFactory;

        public RateService(
            IRedisCacher cacher,
            IDbContextScopeFactory dbContextFactory
        )
        {
            _cacher = cacher;
            _dbContextFactory = dbContextFactory;
        }
        public ApiRateLimit GetAllowed(User user)
        {
            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                var rate = context.Users.Where(x => x.Id == user.Id).Select(x=>x.RateLimit).FirstOrDefault();
                return rate;
            }
        }

        public void ApplyGlobal(ApiRateLimit quota)
        {
            SetAvailabilityForUser("global", quota);
        }

        public ApiRateLimit GetCurrentUsageForUser(User user)
        {
            var key = $"rates:current:{user.UserName}";
            var cachedValue = _cacher.GetHashAsDict(key);
            if (cachedValue == null)
            {
                return new ApiRateLimit();
            }
            else
            {
                var limit = new ApiRateLimit { };
                limit.Weekly = (int)cachedValue["weekly_usage"];
                limit.Monthly = (int)cachedValue["monthly_usage"];
                limit.Daily = (int)cachedValue["daily_usage"];
                return limit;
            }
        }

        public ApiRateLimit GetCurrentQuotaLeftForUser(User user)
        {
            if (user == null)
            {
                return null;
            }
            var key = $"rates:allowed:{user.UserName}";
            var cachedValue = _cacher.GetHashAsDict(key);
            ApiRateLimit allowed = null;
            if (cachedValue == null)
            {
                var current = GetAllowed(user);
                SetAvailabilityForUser(user.UserName, current);
                allowed = current;
            }
            else
            {
                var limit = new ApiRateLimit { };
                limit.Weekly = (int)cachedValue["weekly_usage"];
                limit.Monthly = (int)cachedValue["monthly_usage"];
                limit.Daily = (int)cachedValue["daily_usage"];
                allowed = limit;
            }
            var used = GetCurrentUsageForUser(user);
            var left = allowed - used;
            return left;
        }

        public void ApplyDefaultForUser(User user)
        {
            var key = $"rates:allowed:{user.UserName}";
            var cachedValue = _cacher.GetHashAsDict(key);
            if (cachedValue == null)
            {
                var defaultRate = GetAllowed(user);
                SetAvailabilityForUser(user.FirstName, defaultRate);
            }
        }

        public void SetAvailabilityForUser(string username, ApiRateLimit rate)
        {
            var key = $"rates:allowed:{username}";
            var dict = new Dictionary<string, string>();
            dict["weekly_usage"] = rate.Weekly.ToString();
            dict["monthly_usage"] = rate.Monthly.ToString();
            dict["daily_usage"] = rate.Daily.ToString();
            _cacher.SetHash(key, dict);

            var currentKey = $"rates:current:{username}";
            var currentUsage = _cacher.GetHashAsDict(currentKey);
            if (currentUsage == null)
            {
                var limit = new Dictionary<string, string>();
                limit["weekly_usage"] = 0.ToString();
                limit["monthly_usage"] = 0.ToString();
                limit["daily_usage"] = 0.ToString();
                _cacher.SetHash(currentKey, limit);
            }
        }


    }
}