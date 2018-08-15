using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Netlyt.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Netlyt.Service.Cloud
{
    public class SlaveConnector
    {
        private ConnectionFactory _factory;
        public bool Running { get; set; }
        private IConnection connection;
        private IModel channel;

        public SlaveConnector(IConfiguration config)
        {
            var mqConfig = MqConfig.GetConfig(config);
            _factory = new ConnectionFactory()
            {
                HostName = mqConfig.Host,
                UserName = mqConfig.User,
                Password = mqConfig.Password,
                Port = mqConfig.Port
            };
        }

        public void Run()
        {
            Running = true;
            connection = _factory.CreateConnection();
            channel = connection.CreateModel();
            channel.QueueDeclare(queue: Queues.Notification,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += OnNotification;
            channel.BasicConsume(queue: Queues.Notification,
                autoAck: true,
                consumer: consumer);
        }

        public void Send(string message)
        {
            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(exchange: "",
                routingKey: Queues.Notification,
                basicProperties: null,
                body: body);
        }

        private void OnNotification(object sender, BasicDeliverEventArgs e)
        {
            var body = e.Body;
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine(" [x] Received {0}", message);
        }
    }
}