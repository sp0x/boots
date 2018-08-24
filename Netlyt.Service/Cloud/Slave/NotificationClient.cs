using System;
using System.Text;
using Netlyt.Service.Cloud.Auth;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Netlyt.Service.Cloud.Slave
{
    public class NotificationClient : NotificationExchange
    {
        public NotificationClient(IModel channel) : base(channel)
        {
            channel.QueueDeclare(queue: Queues.Notification,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += OnNotification;
            channel.BasicConsume(queue: Queues.Notification,
                autoAck: false, consumer: consumer);
        }

        #region Handlers
        
        private void OnNotification(object sender, BasicDeliverEventArgs e)
        {
            var props = e.BasicProperties;
            var body = e.Body;
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine(" [x] Received {0}", message);
            Channel.BasicAck(e.DeliveryTag, false);
        }

        #endregion

        public void Send(string message)
        {
            var props = Channel.CreateBasicProperties();
            props.Persistent = true;
            var body = Encoding.UTF8.GetBytes(message);
            Send(Routes.MessageNotification, props, body);
        }

        public void Send(string routingKey, JToken body)
        {
            var props = Channel.CreateBasicProperties();
            props.Persistent = true;
            var bodyBytes = Encoding.UTF8.GetBytes(body.ToString());
            Send(routingKey, props, bodyBytes);
        }

        public void Send(string routingKey, IBasicProperties properties, byte[] body)
        {
            Channel.BasicPublish(exchange: Exchanges.Notifications,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);
        } 


    }
}
