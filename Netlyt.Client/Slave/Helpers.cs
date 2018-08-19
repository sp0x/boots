using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Netlyt.Interfaces.Models;

namespace Netlyt.Client.Slave
{
    public class Helpers
    {
        public static NetlytNode GetLocalNode()
        {
            var node = new NetlytNode();
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
