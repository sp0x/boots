using System.Collections.Generic;
using System.Text;
using Netlyt.Interfaces.Cloud;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client.Events;

namespace Netlyt.Service.Cloud.Auth
{
    public class JsonNotification : Ackable, ICloudNodeNotification
    {
        public JToken Body { get; private set; }
        public string Token { get; private set; }
        public Dictionary<string, string> Headers { get; private set; }

        public JsonNotification(ulong deliveryTag) : base(deliveryTag)
        {
            Headers = new Dictionary<string, string>();
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
            if (e.BasicProperties.Headers != null)
            {
                foreach (var pair in e.BasicProperties.Headers)
                {
                    notification.Headers.Add(pair.Key, Encoding.UTF8.GetString(pair.Value as byte[]));
                }
            }
            return notification;
        }

        public string GetHeader(string key, string defaultValue = null)
        {
            return Headers.ContainsKey(key) ? Headers[key] : defaultValue;
        }
    }
}