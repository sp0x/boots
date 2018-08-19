using System;
using System.Collections.Generic;
using System.Text;
using Netlyt.Service.Cloud.Interfaces;
using Newtonsoft.Json.Linq;

namespace Netlyt.Service.Cloud
{
    public static class Extensions
    {
        public static void Reply(this IRPCableExchange exchange, IRpcMessage message, byte[] reply)
        {
            var channel = exchange.Channel;
            var props = exchange.Channel.CreateBasicProperties();
            //Result is realted to the request
            props.CorrelationId = message.CorrelationId;
            channel.BasicPublish(exchange.Name, message.From, false, props, reply);
            channel.BasicAck(message.DeliveryTag, false);
        }
        public static void Reply(this IRPCableExchange exchange, IRpcMessage message, JObject reply)
        {
            var encodedReply = Encoding.UTF8.GetBytes(reply.ToString());
            var channel = exchange.Channel;
            var props = exchange.Channel.CreateBasicProperties();
            //Result is realted to the request
            props.CorrelationId = message.CorrelationId;
            channel.BasicPublish(exchange.Name, message.From, false, props, encodedReply);
            channel.BasicAck(message.DeliveryTag, false);
        }
    }
}
