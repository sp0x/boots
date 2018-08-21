using Newtonsoft.Json.Linq;
using RabbitMQ.Client.Events;

namespace Netlyt.Service.Cloud.Auth
{
    public class JsonNotification : Ackable
    {
        public JToken Body { get; private set; }
        public JsonNotification(ulong deliveryTag) : base(deliveryTag)
        {
        }

        public static JsonNotification FromRequest(BasicDeliverEventArgs e)
        {
            var notification = new JsonNotification(e.DeliveryTag);
            notification.Body = e.GetJson();
            return notification;
        }
    }
}