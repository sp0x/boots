﻿using System;
using System.Threading.Tasks;
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
        public bool Running { get; set; }
        public NetlytNode Node { get; private set; }
        public ApiRateLimit Quota { get; private set; }
        public string Id { get; private set; }

        public NotificationClient NotificationClient
        {
            get { return notificationClient; }
        }

        public SlaveConnector(IConfiguration config, NetlytNode node, IRateService rateService)
        {
            this.Id = Guid.NewGuid().ToString();
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
                if (!this.Node.Equals(NetlytNode.Cloud))
                {
                    var authResult = await authClient.AuthorizeNode(Node);
                    Quota = authResult.Result["quota"].ToObject<ApiRateLimit>();
                    _rateService.ApplyGlobal(Quota);
                    _rateService.SetAvailabilityForUser(authResult.GetUsername().ToString(), Quota);
                }
                else
                {
                    var authResult = await authClient.AuthorizeCloudNode(Node);
                }
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