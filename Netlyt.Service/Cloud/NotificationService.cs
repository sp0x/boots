using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Donut.Integration;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Cloud.Slave;
using Newtonsoft.Json.Linq;

namespace Netlyt.Service.Cloud
{
    public class NotificationService : INotificationService
    {
        private ISlaveConnector _connector;

        public NotificationService(ISlaveConnector connector)
        {
            _connector = connector;
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
                on = DateTime.UtcNow
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
                on = DateTime.UtcNow
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
                on = DateTime.UtcNow
            });
            _connector.NotificationClient.Send(Routes.IntegrationCreated, body);
        }

        public void SendIntegrationViewed(IIntegration viewedIntegration)
        {
            var body = JObject.FromObject(new {id = viewedIntegration.Id, on = DateTime.UtcNow});
            _connector.NotificationClient.Send(Routes.IntegrationViewed, body);
        }

    }
}
