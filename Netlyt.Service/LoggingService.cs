using System;
using System.Collections.Generic;
using System.Linq;
using Donut;
using Donut.Data;
using EntityFramework.DbContextScope.Interfaces;
using Microsoft.EntityFrameworkCore;
using Netlyt.Interfaces.Cloud;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Data;
using Newtonsoft.Json.Linq;

namespace Netlyt.Service
{
    public class LoggingService : ILoggingService
    {
        private IDbContextScopeFactory _contextScope;
        private IUserService _userService;

        public LoggingService(
            IDbContextScopeFactory contextScope,
            IUserService userService)
        {
            _contextScope = contextScope;
            _userService = userService;
        }

        public void OnIntegrationViewed(ICloudNodeNotification notification, JToken body)
        {
            using (var contextSrc = _contextScope.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                String userId = _userService.VerifyUser(body["user_id"].ToString());
                var item = new ActionLog()
                {
                    Created = body["on"].Value<DateTime>(),
                    Name = body["name"].ToString(),
                    UserId = userId,
                    Value = "",
                    InstanceToken = body["token"]?.ToString(),
                    Type = ActionLogType.IntegrationViewed,
                    ObjectId = long.Parse(body["id"].ToString())
                };
                context.Logs.Add(item);
                contextSrc.SaveChanges();
            }
        }

        public void OnPermissionsChanged(ICloudNodeNotification notification, JToken body)
        {
            using (var contextSrc = _contextScope.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                var type = notification.Headers["type"];
                String userId = _userService.VerifyUser(body["user_id"].ToString());
                var item = new ActionLog()
                {
                    Created = body["on"].ToObject< DateTime>(),
                    Name = body["name"]?.ToString(),
                    UserId = userId,
                    Value = body["value"]?.ToString(),
                    InstanceToken = body["token"]?.ToString(),
                    Type = ResolvePermissionLogType(type),
                    ObjectId = long.Parse(body["id"].ToString())
                };
                context.Logs.Add(item);
                contextSrc.SaveChanges();
            }
        }

        private ActionLogType ResolvePermissionLogType(string type)
        {
            switch (type)
            {
                case "create":
                    return ActionLogType.PermissionsCreate;
                case "remove":
                    return ActionLogType.PermissionRemoved;
                default:
                    throw new NotImplementedException();
            }
        }

        public void OnModelCreated(ICloudNodeNotification notification, JToken body)
        {
            using (var contextSrc = _contextScope.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                String userId = _userService.VerifyUser(body["user_id"].ToString());
                var item = new ActionLog()
                {
                    Created = body["on"].Value<DateTime>(),
                    Name = body["name"].ToString(),
                    UserId = userId,
                    Value = body["id"].ToString(),
                    InstanceToken = body["token"]?.ToString(),
                    Type = ActionLogType.ModelCreated,
                    ObjectId = long.Parse(body["id"].ToString())
                };
                context.Logs.Add(item);
                contextSrc.SaveChanges();
            }
        }

        public void OnModelStageUpdate(ICloudNodeNotification notification, JToken body)
        {
            using (var contextSrc = _contextScope.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                String userId = _userService.VerifyUser(body["user_id"].ToString());
                var item = new ActionLog()
                {
                    Created = body["on"].Value<DateTime>(),
                    Name = body["name"].ToString(),
                    UserId = userId,
                    Value = body["id"].ToString(),
                    InstanceToken = body["token"]?.ToString(),
                    Type = ActionLogType.ModelStageUpdate,
                    ObjectId = long.Parse(body["id"].ToString())
                };
                context.Logs.Add(item);
                contextSrc.SaveChanges();
            }
        }

        public void OnModelTrained(ICloudNodeNotification notification, JToken body)
        {
            using (var contextSrc = _contextScope.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                String userId = _userService.VerifyUser(body["user_id"].ToString());
                var item = new ActionLog()
                {
                    Created = body["on"].Value<DateTime>(),
                    Name = body["name"].ToString(),
                    UserId = userId,
                    Value = body["id"].ToString(),
                    InstanceToken = body["token"]?.ToString(),
                    Type = ActionLogType.ModelTrained,
                    ObjectId = long.Parse(body["id"].ToString())
                };
                context.Logs.Add(item);
                contextSrc.SaveChanges();
            }
        }

        public void OnQuotaSync(ICloudNodeNotification notification, JToken body)
        {
            using (var contextSrc = _contextScope.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                String userId = _userService.VerifyUser(body["user_id"].ToString());
                var item = new ActionLog()
                {
                    Created = body["on"].Value<DateTime>(),
                    Name = body["name"].ToString(),
                    UserId = userId,
                    Value = body["usage"].ToString(),
                    InstanceToken = body["token"]?.ToString(),
                    Type = ActionLogType.QuotaSynced
                };
                context.Logs.Add(item);
                contextSrc.SaveChanges();
            }
        }



        public void OnIntegrationCreated(ICloudNodeNotification notification, JToken body)
        {
            using (var contextSrc = _contextScope.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                String userId = _userService.VerifyUser(body["user_id"].ToString());
                var item = new ActionLog()
                {
                    Created = body["on"].Value<DateTime>(),
                    Name = body["name"].ToString(),
                    UserId = userId,
                    Value = "",
                    InstanceToken = body["token"]?.ToString(),
                    Type = ActionLogType.IntegrationCreated,
                    ObjectId = long.Parse(body["id"].ToString())
                };
                context.Logs.Add(item);

                contextSrc.SaveChanges();
            }
        }

        public IEnumerable<ActionLog> GetIntegrationLogs(DataIntegration ign)
        {
            using (var contextSrc = _contextScope.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                var items = context.Logs
                    .Where(x =>
                    x.ObjectId == ign.Id && (x.Type == ActionLogType.IntegrationCreated ||
                                             x.Type == ActionLogType.IntegrationViewed))
                    .Include(x=>x.User);
                return items;
            }
        }
    }
}