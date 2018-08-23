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

        public NotificationService(
            ISlaveConnector connector,
            IIntegrationRepository integrations,
            IDbContextScopeFactory dbContextFactory)
        {
            _connector = connector;
            _integrations = integrations;
            _dbContextFactory = dbContextFactory;
        }

        public void SendNotification(JToken body)
        {
            var notificationClient = _connector.NotificationClient;
            notificationClient.Send(Routes.MessageNotification, body);
        }

        public void SendRegisteredNotification(User user)
        {
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
        public void SendNewIntegrationSummary(IIntegration newIntegration)
        {
            var body = JObject.FromObject(new
            {
                username = newIntegration.Owner.UserName,
                user_id = newIntegration.Owner.Id,
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

        public void SendIntegrationViewed(long viewedIntegrationId)
        {

            using (var contextSrc = _dbContextFactory.Create())
            {
                var integration = _integrations.GetById(viewedIntegrationId).FirstOrDefault();
                var body = JObject.FromObject(new
                {
                    id = viewedIntegrationId,
                    on = DateTime.UtcNow,
                    user_id = integration.Owner.Id,
                    name = integration.Name,
                    token = _connector.AuthenticationClient.AuthenticationToken
                });
                _connector.NotificationClient.Send(Routes.IntegrationViewed, body);
            }

            
        }

        public void SendModelCreated(Model newModel)
        {
            throw new NotImplementedException();
        }

        public void SendModelBuilding(Model model, JToken trainingTask)
        {
            throw new NotImplementedException();
        }

        public void SendModelTrained(Model model, List<ModelTrainingPerformance> targetPerformances)
        {
            throw new NotImplementedException();
        }
    }
}
