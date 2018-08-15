using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Netlyt.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Netlyt.Service.Cloud
{
    public class MasterServer
    {
        private ConnectionFactory _factory;
        public bool Running { get; set; }
        public MasterServer(IConfiguration config)
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

        public Task Run()
        {
            Running = true;
            return Task.Run(() =>
            {
                using (var connection = _factory.CreateConnection())
                {
                    using (var channel = connection.CreateModel())
                    {
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
                        while (this.Running)
                        {
                            System.Threading.Thread.Sleep(1);
                        }
                    }
                }
            });
        }

        private void OnNotification(object sender, BasicDeliverEventArgs e)
        {
            var body = e.Body;
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine(" [x] Received {0}", message);
        }
    }
}
