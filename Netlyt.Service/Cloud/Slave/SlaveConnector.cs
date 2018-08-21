using System;
using System.Threading.Tasks;
using Donut;
using Microsoft.Extensions.Configuration;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using RabbitMQ.Client;

namespace Netlyt.Service.Cloud.Slave
{
    public class SlaveConnector : IDisposable, ISlaveConnector
    {
        private ConnectionFactory _factory;
        private IConnection connection;
        private IModel channel;
        private NodeAuthClient authClient;
        private NotificationClient notificationClient;
        private IRateService _rateService;
        private IIntegrationService _integrations;
        public bool Running { get; set; }
        public ICloudNodeService _cloudNodeService { get; private set; }
        public ApiRateLimit Quota { get; private set; }
        public string Id { get; private set; }

        public NotificationClient NotificationClient
        {
            get { return notificationClient; }
        }

        public NodeAuthClient AuthenticationClient
        {
            get { return authClient; }
        }

        public SlaveConnector(
            IConfiguration config,
            ICloudNodeService cloudNodeService, 
            IRateService rateService
            //IIntegrationService integrationService
            )
        {
            this.Id = Guid.NewGuid().ToString();
            _cloudNodeService = cloudNodeService;
            var mqConfig = MqConfig.GetConfig(config);
            _rateService = rateService;
            //_integrations = integrationService;
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
                var node = _cloudNodeService.ResolveLocal();
                if (!node.Equals(NetlytNode.Cloud))
                {
                    var authResult = await authClient.AuthorizeNode(node);
                    Quota = authResult.Result["quota"].ToObject<ApiRateLimit>();
                    _rateService.ApplyGlobal(Quota);
                    _rateService.SetAvailabilityForUser(authResult.GetUsername().ToString(), Quota);
                }
                else
                {
                    var authResult = await authClient.AuthorizeCloudNode(node);
                }
            }
            catch (AuthenticationFailed authFailed)
            {
                Console.WriteLine("You're not authenticated.");
                Environment.Exit(1);
            }
            notificationClient = new NotificationClient(channel);
        }

        #region Handlers

        #endregion

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