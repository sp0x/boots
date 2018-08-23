using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Netlyt.Service.Cloud.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client.Events;

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
            channel.BasicPublish(exchange.Name, message.From, true, props, reply);
            channel.BasicAck(message.DeliveryTag, false);
        }

        public static void Ack(this IRPCableExchange exchange, IAckable message)
        {
            var channel = exchange.Channel;
            channel.BasicAck(message.DeliveryTag, false);
        }

        public static void Ack(this IRPCableExchange exchange, BasicDeliverEventArgs message)
        {
            var channel = exchange.Channel;
            channel.BasicAck(message.DeliveryTag, false);
        }

        public static void Reply(this IRPCableExchange exchange, IRpcMessage message, JObject reply)
        {
            var encodedReply = Encoding.UTF8.GetBytes(reply.ToString());
            var channel = exchange.Channel;
            var props = exchange.Channel.CreateBasicProperties();
            //Result is realted to the request
            props.CorrelationId = message.CorrelationId;
            channel.BasicPublish(exchange.Name, message.From, true, props, encodedReply);
            channel.BasicAck(message.DeliveryTag, false);
        }

        public static JToken GetJson(this BasicDeliverEventArgs e)
        {
            var reader = new JsonTextReader(new StreamReader(new MemoryStream(e.Body)));
            var js = JObject.ReadFrom(reader);//JsonSerializer.Create().Deserialize(reader);
            return js;
        }
    }
}
