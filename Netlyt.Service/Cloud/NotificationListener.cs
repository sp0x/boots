using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Netlyt.Service.Cloud
{
    public class NotificationListener
    {
        private IModel channel;

        public NotificationListener(IModel channel)
        {
            this.channel = channel;
            channel.ExchangeDeclare(exchange: Exchanges.Notifications,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);
            channel.QueueDeclare(queue: Queues.MessageNotification, 
                durable: true,
                exclusive: false, 
                autoDelete: false);
            var specs = new Dictionary<string, object>();
            specs["x-match"] = "any";
            //specs["type"] = "";
            channel.QueueBind(queue: Queues.MessageNotification,
                exchange: Exchanges.Notifications,
                routingKey: Routes.MessageNotification,
                arguments: specs);
            ConsumeQuotaNotifications();
        }

        private void ConsumeQuotaNotifications()
        {
            var requestConsumer = new EventingBasicConsumer(channel);
            requestConsumer.Received += OnQuotaNotification;
            channel.BasicConsume(queue: Queues.MessageNotification,
                autoAck: false,
                consumer: requestConsumer);
        }

        private void OnQuotaNotification(object sender, BasicDeliverEventArgs e)
        {
            channel.BasicAck(e.DeliveryTag, false);
        }
    }
}
