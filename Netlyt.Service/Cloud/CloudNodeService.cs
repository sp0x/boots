using System;
using System.Collections.Generic;
using System.Text;
using Netlyt.Interfaces.Models;

namespace Netlyt.Service.Cloud
{
    public class CloudNodeService : ICloudNodeService
    {
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
    }
}
