using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Donut;
using Donut.Orion;
using EntityFramework.DbContextScope.Interfaces;
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
        private IUserService _userService;
        private IIntegrationService _integrations;
        private ILoggingService _loggingService;
        private ICloudNodeService _cloudNodeService;
        private IDbContextScopeFactory _contxtScopeFactory;
        private PermissionService _permissions;
        public bool Running { get; set; }
        public NotificationListener NotificationListener { get; private set; }
        public AuthListener AuthListener { get; private set; }
        public CloudMasterServer(IConfiguration config, 
            ICloudAuthenticationService authService,
            IRateService rateService,
            IUserService userService,
            IIntegrationService integrations,
            ILoggingService loggingService,
            ICloudNodeService cloudNodeService,
            IDbContextScopeFactory contextScopeFactory,
            PermissionService permissionService)
        {
            _permissions = permissionService;
            _contxtScopeFactory = contextScopeFactory;
            _cloudNodeService = cloudNodeService;
            var mqConfig = MqConfig.GetConfig(config);
            _userService = userService;
            _authService = authService;
            _rateService = rateService;
            _integrations = integrations;
            _loggingService = loggingService;
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
                        NotificationListener.OnIntegrationCreated += NotificationListener_OnIntegrationCreated;
                        NotificationListener.OnIntegrationViewed += NotificationListener_OnIntegrationViewed;
                        NotificationListener.OnPermissionsUpdated += NotificationListener_OnPermissionsUpdated;
                        AuthListener.Start();
                        NotificationListener.Start();
                        while (this.Running)
                        {
                            System.Threading.Thread.Sleep(1);
                        }
                    }
                }
            });
        }

        private void NotificationListener_OnPermissionsUpdated(object sender, JsonNotification e)
        {
            _loggingService.OnPermissionsChanged(e, e.Body);
            if (_cloudNodeService.ShouldSync("permission", e))
            {
                _permissions.OnRemotePermissionUpdated(e, e.Body);
            }
            NotificationListener.Ack(e);
        }

        private void NotificationListener_OnIntegrationViewed(object sender, JsonNotification e)
        {
            _loggingService.OnIntegrationViewed(e, e.Body);
            NotificationListener.Ack(e);
        }

        private void NotificationListener_OnIntegrationCreated(object sender, JsonNotification e)
        {
            if (_cloudNodeService.ShouldSync("integration", e))
            {
                _integrations.OnRemoteIntegrationCreated(e, e.Body);
            }
            _loggingService.OnIntegrationCreated(e, e.Body);
            NotificationListener.Ack(e);
        }

        private void AuthListener_UserAuthenticationRequested(object sender, UserLoginRequest e)
        {
            using (var contextSrc = _contxtScopeFactory.Create())
            {
                User user = _userService.GetUserByLogin(e.Email, e.Password);
                if (user == null)
                {
                    throw new Exception("Invalid login.");
                }
                else
                {
                    var userData = GetUserData(user);
                    var reply = JObject.FromObject(new
                    {
                        user = userData,
                        success = true
                    });
                    AuthListener.Reply(e, reply);
                }
            }

        }

        private object GetUserData(User user)
        {
            var userKeys = _userService.GetApiKeys(user);
            return new
            {
                user.Id,
                user.SecurityStamp,
                user.ConcurrencyStamp,
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
                user.AccessFailedCount,
                ApiKeys = user.ApiKeys.Select(x=> new
                    {
                        Api = new
                        {
                            x.Api.AppId,
                            x.Api.AppSecret,
                            x.Api.Endpoint,
                            x.Api.Type
                        },
                        x.UserId,
                        x.ApiId
                    }),
                Organization = new
                {
                    user.Organization.Id,
                    user.Organization.Name,
                    user.Organization.ApiKey
                }
            };
        }

        private void AuthListener_AuthenticationRequested(object sender, AuthenticationRequest e)
        {
            using (var contextSrc = _contxtScopeFactory.Create())
            {
                var result = _authService.Authenticate(e);
                var nodeRole = result.Role.ToString();
                var userData = result.Role == NodeRole.Slave ? GetUserData(result.User) : null;
                var reply = JObject.FromObject(new
                {
                    token = result.Token,
                    success = result.Authenticated,
                    quota = _rateService.GetCurrentQuotaLeftForUser(result.User),
                    role = nodeRole,
                    user = userData
                });
                AuthListener.Reply(e, reply);
            }
        }
    }
    
}
