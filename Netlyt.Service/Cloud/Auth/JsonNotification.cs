﻿using Netlyt.Interfaces.Cloud;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client.Events;

namespace Netlyt.Service.Cloud.Auth
{
    public class JsonNotification : Ackable, ICloudNodeNotification
    {
        public JToken Body { get; private set; }
        public string Token { get; private set; }
        public JsonNotification(ulong deliveryTag) : base(deliveryTag)
        {
        }

        public static JsonNotification FromRequest(BasicDeliverEventArgs e)
        {
            var notification = new JsonNotification(e.DeliveryTag);
            notification.Body = e.GetJson();
            if (!(notification.Body as JObject).ContainsKey("token"))
            {
                throw new MissingToken("Notifications require a token.");
            }
            var token = notification.Body["token"].ToString();
            notification.Token = token;
            return notification;
        }
    }
}