using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Cloud;
using Netlyt.Service.Cloud.Auth;
using Netlyt.Service.Cloud.Interfaces;
using Netlyt.Service.Data;

namespace Netlyt.Master
{
    public class CloudAuthenticationService : ICloudAuthenticationService
    {
        private ManagementDbContext _dbContext;

        public CloudAuthenticationService(ManagementDbContext dbContext)
        {
            _dbContext = dbContext;
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
                var authMatch = _dbContext.ApiKeys.FirstOrDefault(x => x.AppId == authRequest.ApiKey
                                                                       && x.AppSecret == authRequest.ApiSecret);
                if (authMatch != null)
                {
                    var token = GenerateAuthenticationToken(authMatch);
                    var apiUser = authMatch.Users.FirstOrDefault();

                    output = new NodeAuthenticationResult(token, apiUser?.User);
                }
            }
            return output;
        }

        private string GenerateCloudToken(string name)
        {
            var output = Guid.NewGuid().ToString();
            var newAuthorization = new CloudAuthorizationEvent()
            {
                Role = NodeRole.Cloud,
                Name = name,
                CreatedOn = DateTime.UtcNow,
                Token = output
            };
            _dbContext.CloudAuthorizations.Add(newAuthorization);
            return output;
        }

        private string GenerateAuthenticationToken(ApiAuth auth)
        {
            var output = Guid.NewGuid().ToString();
            var newAuthorization = new CloudAuthorizationEvent()
            {
                Role = NodeRole.Slave,
                ApiKey = auth,
                CreatedOn = DateTime.UtcNow,
                Token = output
            };
            _dbContext.CloudAuthorizations.Add(newAuthorization);
            return output;
        }
    }
}
