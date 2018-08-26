using System;
using System.Collections.Generic;
using System.Linq;
using Donut.Integration;
using Donut.Models;
using EntityFramework.DbContextScope.Interfaces;
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

        public NotificationService(
            ISlaveConnector connector,
            IIntegrationRepository integrations,
            IDbContextScopeFactory dbContextFactory,
            IModelRepository models)
        {
            _connector = connector;
            _integrations = integrations;
            _dbContextFactory = dbContextFactory;
            _models = models;
        }

        private void CheckAuthClient()
        {
            if (_connector.AuthenticationClient == null)
            {
                throw new Exception("Node not authorized");
            }
        }

        public void SendNotification(JToken body)
        {
            var notificationClient = _connector.NotificationClient;
            notificationClient.Send(Routes.MessageNotification, body);
        }

        public void SendRegisteredNotification(User user)
        {
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
            CheckAuthClient();
            var body = JObject.FromObject(new
            {
                username = user.UserName,
                user_id = user.Id,
                name = newIntegration.Name,
                id = newIntegration.Id,
                fields = newIntegration.Fields.Select(x => new
                {
                    x.Name,
                    x.Id,
                    x.TargetType,
                    x.Type,
                    x.DataEncoding
                }),
                ts_column = newIntegration.DataTimestampColumn,
                ix_column = newIntegration.DataIndexColumn,
                on = DateTime.UtcNow,
                token = _connector.AuthenticationClient.AuthenticationToken
            });
            _connector.NotificationClient.Send(Routes.IntegrationCreated, body);
        }

        public void SendIntegrationViewed(long viewedIntegrationId, string userId)
        {
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
    }
}
