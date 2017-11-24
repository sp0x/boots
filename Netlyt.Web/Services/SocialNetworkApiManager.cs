using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CsQuery.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using nvoid.db.Extensions;
using nvoid.Integration;
using Newtonsoft.Json.Linq;
using Netlyt.Service.Integration;

namespace Netlyt.Web.Services
{
    // This class is used by the application to send Email and SMS
    // when you turn on two-factor authentication in ASP.NET Identity.
    // For more details see this link http://go.microsoft.com/fwlink/?LinkID=532713
    public class SocialNetworkApiManager
    { 
        public SocialNetworkApiManager()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="name"></param>
        /// <param name="appId"></param>
        /// <param name="appSecret"></param>
        public void RegisterNetwork(ISession session, string name, string appId, string appSecret)
        {
            var socnetValue = JObject.FromObject(new { appId, secret = appSecret });
            session.SetString($"__socn_{name}", socnetValue.ToString());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public ApiAuth GetRegisteredNetwork(ISession session, string name)
        {
            var sessionNetworkValue = session.GetString($"__socn_{name}");
            if (string.IsNullOrEmpty(sessionNetworkValue)) return null;
            var sessionBlob = JObject.Parse(sessionNetworkValue);
            return new ApiAuth
            {
                AppId = sessionBlob["appId"].ToString(),
                AppSecret = sessionBlob["secret"].ToString()
            };
        }
    }
}
