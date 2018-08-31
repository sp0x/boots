using System;
using System.Text;
using Netlyt.Interfaces.Models;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client.Events;

namespace Netlyt.Service.Cloud.Auth
{
    public class AuthenticationResponse : RpcMessage
    {
        public User User { get; private set; }
        public JObject Result { get; private set; }
        public NodeRole AsRole { get; private set; } = NodeRole.Slave;

        public static AuthenticationResponse FromRequest(BasicDeliverEventArgs e)
        {
            var rq = new AuthenticationResponse(e.BasicProperties.ReplyTo, e.BasicProperties.CorrelationId, e.DeliveryTag);
            var body = JObject.Parse(Encoding.UTF8.GetString(e.Body));
            rq.Result = body;
            rq.AsRole = body.ContainsKey("role") ? (NodeRole)Enum.Parse(typeof(NodeRole), body["role"].ToString()) : NodeRole.Slave;
            rq.User = body["user"].ToObject<User>();
            return rq;
        }

        public AuthenticationResponse(string replyTo, string correlationId, ulong deliveryTag) : base(replyTo, correlationId, deliveryTag)
        {
        } 
    }
}