using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Donut.Orion;
using Microsoft.Extensions.Configuration;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
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
        private UserService _userService;
        public bool Running { get; set; }
        public NotificationListener NotificationListener { get; private set; }
        public AuthListener AuthListener { get; private set; }
        public CloudMasterServer(IConfiguration config, 
            ICloudAuthenticationService authService,
            IRateService rateService,
            UserService userService)
        {
            var mqConfig = MqConfig.GetConfig(config);
            _userService = userService;
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
                        AuthListener.UserAuthenticationRequested += AuthListener_UserAuthenticationRequested;
                        while (this.Running)
                        {
                            System.Threading.Thread.Sleep(1);
                        }
                    }
                }
            });
        }

        private void AuthListener_UserAuthenticationRequested(object sender, UserLoginRequest e)
        {
            User user = _userService.GetUserByLogin(e.Email, e.Password).Result;
            if (user == null)
            {
                throw new Exception("Invalid login.");
            }
            else
            {
                var userData = new
                {
                    user.Id,
                    user.FirstName,
                    user.Email,
                    user.EmailConfirmed,
                    user.LastName,
                    user.LockoutEnabled,
                    user.LockoutEnd,
                    user.PasswordHash,
                    user.PhoneNumber,
                    user.NormalizedUserName,
                    user.UserName,
                    user.NormalizedEmail,
                    ApiKeys = AnonimyzeKeys(user.ApiKeys),
//                    Organization = new
//                    {
//                        user.Organization.Id,
//                        user.Organization.Name,
//                        user.Organization.ApiKey
//                    }
                };
                var reply = JObject.FromObject(new{
                    user = userData,
                    success = true
                });
                AuthListener.Reply(e, reply);
            }
        }

        private IEnumerable<object> AnonimyzeKeys(ICollection<ApiUser> userApiKeys)
        {
            return userApiKeys.Select(key => new
            {
                Api=new
                {
                    key.Api.AppId,
                    key.Api.AppSecret
                },
                ApiId = key.ApiId,
                key.UserId
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
