using System;
using System.Collections.Generic;
using System.Linq;
using Donut.Data;
using Donut.Integration;
using Donut.Models;
using EntityFramework.DbContextScope.Interfaces;
using Netlyt.Data.ViewModels;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Cloud.Slave;
using Netlyt.Service.Repisitories;
using Newtonsoft.Json.Linq;

namespace Netlyt.Service.Cloud
{
    public class NotificationService : INotificationService
    {
        private ISlaveConnector _connector;
        private IIntegrationRepository _integrations;
        private IDbContextScopeFactory _dbContextFactory;
        private IModelRepository _models;
        private ICloudNodeService _nodeResolver;

        public NotificationService(
            ISlaveConnector connector,
            IIntegrationRepository integrations,
            IDbContextScopeFactory dbContextFactory,
            IModelRepository models,
            ICloudNodeService nodeResolver)
        {
            _nodeResolver = nodeResolver;
            _connector = connector;
            _integrations = integrations;
            _dbContextFactory = dbContextFactory;
            _models = models;
        }

        private void CheckAuthClient()
        {
            if (_connector.AuthenticationClient == null || string.IsNullOrEmpty(_connector.AuthenticationClient.AuthenticationToken))
            {
                throw new Exception("Node not authorized");
            }
        }

        public void SendNotification(JToken body)
        {
            if (!_nodeResolver.ShouldNotify("logged in")) return;
            var notificationClient = _connector.NotificationClient;
            notificationClient.Send(Routes.MessageNotification, body);
        }

        public void SendRegisteredNotification(User user)
        {
            if (!_nodeResolver.ShouldNotify("register")) return;
            CheckAuthClient();
            var body = JObject.FromObject(new
            {
                username = user.UserName,
                id = user.Id,
                email = user.Email,
                on = DateTime.UtcNow,
                token = _connector.AuthenticationClient.AuthenticationToken
            });
            _connector.NotificationClient.Send(Routes.UserRegisterNotification, body);
        }

        public void SendLoggedInNotification(User user)
        {
            if (!_nodeResolver.ShouldNotify("login")) return;
            CheckAuthClient();
            var body = JObject.FromObject(new
            {
                username = user.UserName,
                id = user.Id,
                email = user.Email,
                on = DateTime.UtcNow,
                token = _connector.AuthenticationClient.AuthenticationToken
            });
            _connector.NotificationClient.Send(Routes.UserLoginNotification, body);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newIntegration"></param>
        public void SendNewIntegrationSummary(IIntegration newIntegration, User user)
        {
            if (!_nodeResolver.ShouldNotify("integration")) return; 
            CheckAuthClient();
            using (var contextSrc = _dbContextFactory.Create())
            {
                var integration = _integrations.GetById(newIntegration.Id).FirstOrDefault();
                var body = JObject.FromObject(new
                {
                    username = user.UserName,
                    user_id = user.Id,
                    name = integration.Name,
                    id = integration.Id,
                    integration.DataFormatType,
                    integration.FeatureScript,
                    fields = integration.Fields.Select(x => new
                    {
                        x.Name,
                        x.Id,
                        x.Type,
                        x.DataEncoding,
                        x.TargetType,
                        x.DataType,
                        x.DescriptionJson
                    }).ToList(),
                    ts_column = integration.DataTimestampColumn,
                    ix_column = integration.DataIndexColumn,
                    on = DateTime.UtcNow,
                    token = _connector.AuthenticationClient.AuthenticationToken
                });
                _connector.NotificationClient.Send(Routes.IntegrationCreated, body);
            }
        }

        public void SendIntegrationViewed(long viewedIntegrationId, string userId)
        {
            if (!_nodeResolver.ShouldNotify("integration")) return;
            CheckAuthClient();
            using (var contextSrc = _dbContextFactory.Create())
            {
                var integration = _integrations.GetById(viewedIntegrationId).FirstOrDefault();
                var body = JObject.FromObject(new
                {
                    id = viewedIntegrationId,
                    on = DateTime.UtcNow,
                    user_id = userId,
                    name = integration.Name,
                    token = _connector.AuthenticationClient.AuthenticationToken
                });
                _connector.NotificationClient.Send(Routes.IntegrationViewed, body);
            }
        }

        public void SendModelCreated(Model newModel, User user)
        {
            if (!_nodeResolver.ShouldNotify("model")) return;
            CheckAuthClient();
            using (var contextSrc = _dbContextFactory.Create())
            {
                newModel = _models.GetById(newModel.Id).FirstOrDefault();
                var body = JObject.FromObject(new
                {
                    id = newModel.Id,
                    on = DateTime.UtcNow,
                    user_id = user.Id,
                    name = newModel.ModelName,
                    token = _connector.AuthenticationClient.AuthenticationToken
                });
                _connector.NotificationClient.Send(Routes.IntegrationViewed, body);
            }
        }

        public void SendModelBuilding(Model model, User user, JToken trainingTask)
        {
            if (!_nodeResolver.ShouldNotify("model")) return;
            CheckAuthClient();
            using (var contextSrc = _dbContextFactory.Create())
            {
                model = _models.GetById(model.Id).FirstOrDefault();
                var body = JObject.FromObject(new
                {
                    id = model.Id,
                    on = DateTime.UtcNow,
                    user_id = user.Id,
                    name = model.ModelName,
                    token = _connector.AuthenticationClient.AuthenticationToken
                });
                _connector.NotificationClient.Send(Routes.IntegrationViewed, body);
            }
        }

        public void SendModelTrained(Model model, User user, List<ModelTrainingPerformance> targetPerformances)
        {
            if (!_nodeResolver.ShouldNotify("model")) return;
            CheckAuthClient();
            using (var contextSrc = _dbContextFactory.Create())
            {
                model = _models.GetById(model.Id).FirstOrDefault();
                var body = JObject.FromObject(new
                {
                    id = model.Id,
                    on = DateTime.UtcNow,
                    user_id = user.Id,
                    name = model.ModelName,
                    token = _connector.AuthenticationClient.AuthenticationToken
                });
                _connector.NotificationClient.Send(Routes.IntegrationViewed, body);
            }
        }

        public void SendPermissionRemoved(User createdBy, Permission permission)
        {
            if (!_nodeResolver.ShouldNotify("permission")) return;
            CheckAuthClient();
            var body = JObject.FromObject(new
            {
                on = DateTime.UtcNow,
                user_id = createdBy.Id,
                token = _connector.AuthenticationClient.AuthenticationToken,
                id = permission.Id
            });
            var headers = new Dictionary<string, string>();
            headers["type"] = "remove";
            _connector.NotificationClient.Send(Routes.PermissionsUpdate, body, headers);
        }

        public void SendPermissionCreated(User createdBy, Permission newPerm)
        {
            if (!_nodeResolver.ShouldNotify("permission")) return;
            CheckAuthClient();
            using (var contextSrc = _dbContextFactory.Create())
            {
                var body = JObject.FromObject(new
                {
                    newPerm.CanModify,
                    newPerm.CanRead,
                    ShareWith = new
                    {
                        newPerm.ShareWith.Id
                    },
                    OwnerId = newPerm.Owner.Id,
                    newPerm.DataIntegrationId,
                    newPerm.ModelId,
                    on = DateTime.UtcNow,
                    user_id = createdBy.Id,
                    token = _connector.AuthenticationClient.AuthenticationToken,
                    id = newPerm.Id,
                    value = (newPerm.CanRead ? "r" :"") + (newPerm.CanModify ? "w" : "") + " " + (newPerm.ShareWith.Name)
                });
                var headers = new Dictionary<string, string>();
                headers["type"] = "create";
                _connector.NotificationClient.Send(Routes.PermissionsUpdate, body, headers);
            }
        }
    }
}
