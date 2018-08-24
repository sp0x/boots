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

        public void OnPermissionsChanged(JToken body)
        {
            using (var contextSrc = _contextScope.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                var item = new ActionLog()
                {
                    Created = body["on"].Value<DateTime>(),
                    Name = body["name"].ToString(),
                    UserId = body["user_id"].ToString(),
                    Value = body["value"].ToString(),
                    InstanceToken = body["token"]?.ToString(),
                    Type = ActionLogType.PermissionsSet
                };
                context.Logs.Add(item);
                contextSrc.SaveChanges();
            }
        }

        public void OnModelCreated(JToken body)
        {
            using (var contextSrc = _contextScope.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                var item = new ActionLog()
                {
                    Created = body["on"].Value<DateTime>(),
                    Name = body["name"].ToString(),
                    UserId = body["user_id"].ToString(),
                    Value = body["id"].ToString(),
                    InstanceToken = body["token"]?.ToString(),
                    Type = ActionLogType.ModelCreated
                };
                context.Logs.Add(item);
                contextSrc.SaveChanges();
            }
        }

        public void OnModelStageUpdate(JToken body)
        {
            using (var contextSrc = _contextScope.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                var item = new ActionLog()
                {
                    Created = body["on"].Value<DateTime>(),
                    Name = body["name"].ToString(),
                    UserId = body["user_id"].ToString(),
                    Value = body["id"].ToString(),
                    InstanceToken = body["token"]?.ToString(),
                    Type = ActionLogType.ModelStageUpdate
                };
                context.Logs.Add(item);
                contextSrc.SaveChanges();
            }
        }

        public void OnModelTrained(JToken body)
        {
            using (var contextSrc = _contextScope.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                var item = new ActionLog()
                {
                    Created = body["on"].Value<DateTime>(),
                    Name = body["name"].ToString(),
                    UserId = body["user_id"].ToString(),
                    Value = body["id"].ToString(),
                    InstanceToken = body["token"]?.ToString(),
                    Type = ActionLogType.ModelTrained
                };
                context.Logs.Add(item);
                contextSrc.SaveChanges();
            }
        }

        public void OnQuotaSync(JToken body)
        {
            using (var contextSrc = _contextScope.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                var item = new ActionLog()
                {
                    Created = body["on"].Value<DateTime>(),
                    Name = body["name"].ToString(),
                    UserId = body["user_id"].ToString(),
                    Value = body["usage"].ToString(),
                    InstanceToken = body["token"]?.ToString(),
                    Type = ActionLogType.QuotaSynced
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