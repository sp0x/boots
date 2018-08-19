using System.Text;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client.Events;

namespace Netlyt.Service.Cloud.Auth
{
    public class AuthenticationResponse : RpcMessage
    {
        public static AuthenticationResponse FromRequest(BasicDeliverEventArgs e)
        {
            var rq = new AuthenticationResponse(e.BasicProperties.ReplyTo, e.BasicProperties.CorrelationId, e.DeliveryTag);
            var body = JObject.Parse(Encoding.UTF8.GetString(e.Body));
            rq.Result = body;
            return rq;
        }
        public JObject Result { get; set; }

        public AuthenticationResponse(string replyTo, string correlationId, ulong deliveryTag) : base(replyTo, correlationId, deliveryTag)
        {
        }
    }
}