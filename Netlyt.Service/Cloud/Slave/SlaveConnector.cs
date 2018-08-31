
using System;
using System.Threading;
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
    /// Connects to the master cloud.
    /// TODO: Fix thread context switching..
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
        private IUserService _userService;
        public bool Running { get; set; }
        public ICloudNodeService _cloudNodeService { get; private set; }
        public ApiRateLimit Quota { get; private set; }
        public string Id { get; private set; }
        public User User { get; private set; }
        private CancellationToken _startCancellation;
        private MqConfig _mqConfig;

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
            IDbContextScopeFactory dbContextFactory,
            IUserService userService
            )
        {
            _userService = userService;
            _dbContextFactory = dbContextFactory;
            _users = users;
            this.Id = Guid.NewGuid().ToString();
            _cloudNodeService = cloudNodeService;
            _mqConfig = MqConfig.GetConfig(config);
            _rateService = rateService;
            _modelService = modelService;
            _integrations = integrationService;
            _factory = new ConnectionFactory()
            {
                HostName = _mqConfig.Host,
                UserName = _mqConfig.User,
                Password = _mqConfig.Password,
                Port = _mqConfig.Port
            };
        }

        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _startCancellation = cancellationToken;
            return Run();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public async Task Run()
        {
            Running = true;
            Console.WriteLine($"Connecting to netlyt cloud @ {_mqConfig}");
            connection = _factory.CreateConnection();
            channel = connection.CreateModel();
            authClient = new NodeAuthClient(channel);
            notificationClient = new NotificationClient(channel);
            try
            {
                var node = _cloudNodeService.ResolveLocal();
                Console.WriteLine("NODE: " + node);
                if (!node.Equals(NetlytNode.Cloud))
                {
                    var authResult = await authClient.AuthorizeNode(node);
                    Quota = authResult.Result["quota"].ToObject<ApiRateLimit>();
                    authResult.User.ApiKeys.Add(new ApiUser(authResult.User, node.ApiKey));
                    var cloudUser = _userService.CreateUser(authResult.User, Quota);
                    User = cloudUser;
                    _rateService.ApplyGlobal(Quota);
                    taskClient = new TaskClient(channel, authClient.AuthenticationToken);
                    taskClient.OnCommand += (sender, e) => { TaskClient_OnCommand(sender, e); };
                    taskClient.Start();
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