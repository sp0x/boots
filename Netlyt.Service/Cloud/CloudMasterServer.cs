using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Donut.Orion;
using Microsoft.Extensions.Configuration;
using Netlyt.Interfaces;
using Netlyt.Service.Cloud.Auth;
using Netlyt.Service.Cloud.Interfaces;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Netlyt.Service.Cloud
{
    public class CloudMasterServer : ICloudMasterServer
    {
        private ConnectionFactory _factory;
        private ICloudAuthenticationService _authService;
        private IRateService _rateService;
        public bool Running { get; set; }
        public NotificationListener NotificationListener { get; private set; }
        public AuthListener AuthListener { get; private set; }
        public CloudMasterServer(IConfiguration config, 
            ICloudAuthenticationService authService,
            IRateService rateService)
        {
            var mqConfig = MqConfig.GetConfig(config);
            _authService = authService;
            _rateService = rateService;
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
                        NotificationListener = new NotificationListener(channel);
                        AuthListener = new AuthListener(channel, AuthMode.Master);
                        AuthListener.AuthenticationRequested += AuthListener_AuthenticationRequested;
                        while (this.Running)
                        {
                            System.Threading.Thread.Sleep(1);
                        }
                    }
                }
            });
        }

        private void AuthListener_AuthenticationRequested(object sender, AuthenticationRequest e)
        {
            var result = _authService.Authenticate(e);
            var nodeRole = result.Role.ToString();
            var reply = JObject.FromObject(new
            {
                token = result.Token,
                success = result.Authenticated,
                quota = _rateService.GetCurrentQuotaLeftForUser(result.User),
                role = nodeRole,
                user = result.User?.UserName
            });
            AuthListener.Reply(e, reply);
        }
    }
    
}
