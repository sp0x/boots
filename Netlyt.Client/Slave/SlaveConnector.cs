using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Donut.Orion;
using Microsoft.Extensions.Configuration;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Service.Cloud;
using Netlyt.Service.Cloud.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Netlyt.Client.Slave
{
    public class SlaveConnector : IDisposable, ISlaveConnector
    {
        private ConnectionFactory _factory;
        public bool Running { get; set; }
        private IConnection connection;
        private IModel channel;
        private NodeAuthClient authClient;
        private NotificationClient notificationClient;
        private IRateService _rateService;
        public NetlytNode Node { get; private set; }
        public ApiRateLimit Quota { get; private set; }

        public SlaveConnector(IConfiguration config, NetlytNode node, IRateService rateService)
        {
            this.Node = node;
            var mqConfig = MqConfig.GetConfig(config);
            _rateService = rateService;
            _factory = new ConnectionFactory()
            {
                HostName = mqConfig.Host,
                UserName = mqConfig.User,
                Password = mqConfig.Password,
                Port = mqConfig.Port
            };
        }

        public async Task Run()
        {
            Running = true;
            connection = _factory.CreateConnection();
            channel = connection.CreateModel();
            authClient = new NodeAuthClient(channel);
            try
            {
                var authResult = await authClient.AuthorizeNode(Node);
                Quota = authResult.Result["quota"].ToObject<ApiRateLimit>();
                _rateService.ApplyGlobal(Quota);
            }
            catch (AuthenticationFailed authFailed)
            {
                Console.WriteLine("You're not authenticated.");
                Environment.Exit(1);
            }
            notificationClient = new NotificationClient(channel);
        }


        public void Send(string message)
        {
            this.notificationClient.Send(message);
        }

        public void Dispose()
        {
            connection?.Dispose();
            channel?.Dispose();
        }
    }
}