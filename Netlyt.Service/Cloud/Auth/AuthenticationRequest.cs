using System;
using System.Text;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Data;
using RabbitMQ.Client.Events;

namespace Netlyt.Service.Cloud.Auth
{
    public class AuthenticationRequest : RpcMessage
    {
        public string Name { get; private set; }
        public string ApiKey { get; private set; }
        public string ApiSecret { get; private set; }
        public NodeRole AsRole { get; private set; } = NodeRole.Slave;


        public AuthenticationRequest(string replyTo, string correlationId, ulong deliveryTag) : base(replyTo, correlationId, deliveryTag)
        {
        }

        public static AuthenticationRequest FromRequest(BasicDeliverEventArgs e)
        {
            var rq = new AuthenticationRequest(e.BasicProperties.ReplyTo, e.BasicProperties.CorrelationId, e.DeliveryTag);
            var body = Encoding.ASCII.GetString(Convert.FromBase64String(Encoding.ASCII.GetString(e.Body)));
            if (!string.IsNullOrEmpty(body) && body.StartsWith("__cloud__;"))
            {
                return FromCloudRequest(e, body);
            }
            var bodyParts = body.Split("//\\\\", StringSplitOptions.None);
            var apiKey = bodyParts[0];
            var apiSecret = bodyParts[1];
            rq.ApiKey = apiKey;
            rq.ApiSecret = apiSecret;
            return rq;
        }

        private static AuthenticationRequest FromCloudRequest(BasicDeliverEventArgs e, string body)
        {
            var cloudName = body.Split(";", StringSplitOptions.None)[1];
            var rq = new AuthenticationRequest(e.BasicProperties.ReplyTo, e.BasicProperties.CorrelationId, e.DeliveryTag);
            rq.AsRole = NodeRole.Cloud;
            rq.Name = cloudName;
            return rq;
        }

    }
}