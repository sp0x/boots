using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityFramework.DbContextScope.Interfaces;
using Netlyt.Interfaces.Cloud;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Cloud.Auth;
using Netlyt.Service.Data;

namespace Netlyt.Service.Cloud
{
    public class CloudNodeService : ICloudNodeService
    {
        private IDbContextScopeFactory _dbContextFactory;

        public CloudNodeService(
            IDbContextScopeFactory dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public NetlytNode ResolveLocal()
        {
            var node = new NetlytNode();
            var nodeType = Environment.GetEnvironmentVariable("NODE_TYPE");
            if (nodeType == NetlytNode.NODE_TYPE_CLOUD)
            {
                return NetlytNode.Cloud;
            }
            else
            {
                var authKey = Environment.GetEnvironmentVariable("NAPI_KEY");
                var authSecret = Environment.GetEnvironmentVariable("NAPI_SECRET");
                var nodeName = Environment.GetEnvironmentVariable("NAPI_NAME");
                if (string.IsNullOrEmpty(authKey)) throw new Exception("Api key is empty");
                if (string.IsNullOrEmpty(authSecret)) throw new Exception("Api secret is empty");
                if (string.IsNullOrEmpty(nodeName)) throw new Exception("Api node name is empty");
                node.ApiKey = new ApiAuth()
                {
                    AppId = authKey,
                    AppSecret = authSecret
                };
                node.Name = nodeName;
                return node;
            }
        }

        public bool ShouldSync(string dataType, ICloudNodeNotification jsonNotification)
        {
            NodeRole loginRole = GetNodeLoginRole(jsonNotification.Token);
            switch (dataType)
            {
                case "integration":
                    return loginRole == NodeRole.Slave;
                case "permission":
                    return loginRole == NodeRole.Slave;
                default:
                    throw new NotImplementedException();
            }
            
        }

        public bool UserHasOnPremInstance(User src)
        {
            using (var ctxScope = _dbContextFactory.Create())
            {
                var context = ctxScope.DbContexts.Get<ManagementDbContext>();
                return context.CloudAuthorizations
                    .Any(x => x.ApiKey.Users
                        .Any(user => user.UserId == src.Id));
            }
        }

        /// <summary>
        /// Gets the login role of a node.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private NodeRole GetNodeLoginRole(string token)
        {
            using (var ctxScope = _dbContextFactory.Create())
            {
                var context = ctxScope.DbContexts.Get<ManagementDbContext>();
                var authentication = context.CloudAuthorizations.FirstOrDefault(x => x.Token == token);
                if(authentication==null) throw new CloudNotAuthenticated();
                return authentication.Role;
            }
        }
    }
}
