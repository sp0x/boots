using System;
using System.Text;
using Netlyt.Service.Data;
using RabbitMQ.Client.Events;

namespace Netlyt.Service.Cloud.Auth
{
    public class AuthenticationRequest : RpcMessage
    {
        public string ApiKey { get; private set; }
        public string ApiSecret { get; private set; }


        public AuthenticationRequest(string replyTo, string correlationId, ulong deliveryTag) : base(replyTo, correlationId, deliveryTag)
        {
        }

        public static AuthenticationRequest FromRequest(BasicDeliverEventArgs e)
        {
            var rq = new AuthenticationRequest(e.BasicProperties.ReplyTo, e.BasicProperties.CorrelationId, e.DeliveryTag);
            var body = Encoding.ASCII.GetString(Convert.FromBase64String(Encoding.ASCII.GetString(e.Body)));
            var bodyParts = body.Split("//\\\\", StringSplitOptions.None);
            var apiKey = bodyParts[0];
            var apiSecret = bodyParts[1];
            rq.ApiKey = apiKey;
            rq.ApiSecret = apiSecret;
            return rq;
        }
    }
}