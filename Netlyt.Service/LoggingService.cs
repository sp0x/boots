using System;
using Donut;
using EntityFramework.DbContextScope.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Data;
using Newtonsoft.Json.Linq;

namespace Netlyt.Service
{
    public class LoggingService : ILoggingService
    {
        private IDbContextScopeFactory _contextScope;

        public LoggingService(
            IDbContextScopeFactory contextScope)
        {
            _contextScope = contextScope;
        }

        public void OnIntegrationViewed(JToken body)
        {
            using (var contextSrc = _contextScope.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                var item = new ActionLog()
                {
                    Created = body["on"].Value<DateTime>(),
                    Name = body["name"].ToString(),
                    UserId = body["user_id"].ToString(),
                    Value = "",
                    InstanceToken = body["token"]?.ToString(),
                    Type = ActionLogType.IntegrationViewed
                };
                context.Logs.Add(item);
                contextSrc.SaveChanges();
            }
        }
        public void OnIntegrationCreated(JToken body)
        {
            using (var contextSrc = _contextScope.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                var item = new ActionLog()
                {
                    Created = body["on"].Value<DateTime>(),
                    Name = body["name"].ToString(),
                    UserId = body["user_id"].ToString(),
                    Value = "",
                    InstanceToken = body["token"]?.ToString(),
                    Type = ActionLogType.IntegrationCreated
                };
                context.Logs.Add(item);
                contextSrc.SaveChanges();
            }
        }
    }
}