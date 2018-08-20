using System;
using System.Collections.Generic;
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

        }

        public void SendRegisteredNotification(User resultItem2)
        {
            throw new NotImplementedException();
        }

        public void SendLoggedInNotification(ClaimsPrincipal httpContextUser)
        {
            throw new NotImplementedException();
        }

        public void SendNewIntegrationSummary(IIntegration newIntegration)
        {
            throw new NotImplementedException();
        }
    }
}
