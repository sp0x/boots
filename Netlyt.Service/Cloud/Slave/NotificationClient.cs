using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Netlyt.Service.Cloud.Slave
{
    public class NotificationClient
    {
        private IModel channel;

        public NotificationClient(IModel channel)
        {
            this.channel = channel;
            channel.QueueDeclare(queue: Queues.Notification,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += OnNotification;
            channel.BasicConsume(queue: Queues.Notification,
                autoAck: false,
                consumer: consumer);
        }

        public void Send(string message)
        {
            var props = channel.CreateBasicProperties();
            props.Persistent = true;
            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(exchange: Exchanges.Notifications,
                routingKey: Routes.MessageNotification,
                basicProperties: props,
                body: body);
        }


        private void OnNotification(object sender, BasicDeliverEventArgs e)
        {
            var props = e.BasicProperties;
            var body = e.Body;
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine(" [x] Received {0}", message);
            channel.BasicAck(e.DeliveryTag, false);
        }

    }
}
