﻿
using System;
using System.Threading.Tasks;
using Donut;
using EntityFramework.DbContextScope.Interfaces;
using Microsoft.Extensions.Configuration;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Repisitories;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Netlyt.Service.Cloud.Slave
{
    /// <summary>
    /// Connects to the master cloud
    /// </summary>
    public class SlaveConnector : IDisposable, ISlaveConnector
    {
        private ConnectionFactory _factory;
        private IConnection connection;
        private IModel channel;
        private NodeAuthClient authClient;
        private NotificationClient notificationClient;
        private TaskClient taskClient;
        private IRateService _rateService;
        private IIntegrationService _integrations;
        private ModelService _modelService;
        private IDbContextScopeFactory _dbContextFactory;
        private IUsersRepository _users;
        public bool Running { get; set; }
        public ICloudNodeService _cloudNodeService { get; private set; }
        public ApiRateLimit Quota { get; private set; }
        public string Id { get; private set; }
        public User User { get; private set; }

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
            IRateService rateService,
            ModelService modelService,
            IIntegrationService integrationService,
            IUsersRepository users,
            IDbContextScopeFactory dbContextFactory
            )
        {
            _dbContextFactory = dbContextFactory;
            _users = users;
            this.Id = Guid.NewGuid().ToString();
            _cloudNodeService = cloudNodeService;
            var mqConfig = MqConfig.GetConfig(config);
            _rateService = rateService;
            _modelService = modelService;
            _integrations = integrationService;
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
            notificationClient = new NotificationClient(channel);
            try
            {
                var node = _cloudNodeService.ResolveLocal();
                if (!node.Equals(NetlytNode.Cloud))
                {
                    var authResult = await authClient.AuthorizeNode(node);
                    Quota = authResult.Result["quota"].ToObject<ApiRateLimit>();
                    using (var ctxSrc = _dbContextFactory.Create())
                    {
                        User = _users.GetByUsername(authResult.Result["username"].ToString());
                    }
                    _rateService.ApplyGlobal(Quota);
                    _rateService.SetAvailabilityForUser(authResult.GetUsername().ToString(), Quota);
                    taskClient = new TaskClient(channel, authClient.AuthenticationToken);
                    taskClient.OnCommand += (sender, e) => { TaskClient_OnCommand(sender, e); };

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
        }


        #region Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task TaskClient_OnCommand(object sender, Tuple<string,BasicDeliverEventArgs> e)
        {
            var exchange = sender as TaskExchange;
            var command = e.Item1;
            switch (command)
            {
                case "train":
                    await _modelService.TrainOnCommand(e.Item2, User);
                    break;
            }
            exchange.Ack(e.Item2);
        }

        

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