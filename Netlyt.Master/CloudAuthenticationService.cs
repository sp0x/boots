using System;
using System.Linq;
using EntityFramework.DbContextScope.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Cloud;
using Netlyt.Service.Cloud.Auth;
using Netlyt.Service.Cloud.Interfaces;
using Netlyt.Service.Data;

namespace Netlyt.Master
{
    public class CloudAuthenticationService : ICloudAuthenticationService
    {
        private IDbContextScopeFactory _dbContextFactory;

        public CloudAuthenticationService(IDbContextScopeFactory dbFactory)
        {
            _dbContextFactory = dbFactory;
        }

        public NodeAuthenticationResult Authenticate(AuthenticationRequest authRequest)
        {
            var output = new NodeAuthenticationResult();
            if (authRequest == null) return output;
            if (authRequest.AsRole == NodeRole.Cloud)
            {
                var token = GenerateCloudToken(authRequest.Name);
                output = new NodeAuthenticationResult(token, null);
                output.Role = NodeRole.Cloud;
            }
            else
            {
                using (var ctxSrc = _dbContextFactory.Create())
                {
                    var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                    var authMatch = context.ApiKeys.FirstOrDefault(x => x.AppId == authRequest.ApiKey
                                                                           && x.AppSecret == authRequest.ApiSecret);
                    if (authMatch != null)
                    {
                        var token = GenerateAuthenticationToken(authMatch);
                        var apiUser = authMatch.Users.FirstOrDefault();

                        output = new NodeAuthenticationResult(token, apiUser?.User);
                    }
                }
            }
            return output;
        }

        private string GenerateCloudToken(string name)
        {
            var output = Guid.NewGuid().ToString();
            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                var newAuthorization = new CloudAuthorizationEvent()
                {
                    Role = NodeRole.Cloud,
                    Name = name,
                    CreatedOn = DateTime.UtcNow,
                    Token = output
                };
                context.CloudAuthorizations.Add(newAuthorization);
                ctxSrc.SaveChanges();
                return output;
            }
        }

        private string GenerateAuthenticationToken(ApiAuth auth)
        {
            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                var output = Guid.NewGuid().ToString();
                var newAuthorization = new CloudAuthorizationEvent()
                {
                    Role = NodeRole.Slave,
                    ApiKey = auth,
                    CreatedOn = DateTime.UtcNow,
                    Token = output
                };
                context.CloudAuthorizations.Add(newAuthorization);
                ctxSrc.SaveChanges();
                return output;
            }
        }
    }
}
