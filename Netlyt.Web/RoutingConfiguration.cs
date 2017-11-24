using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Netlyt.Web
{
    public class RoutingConfiguration
    {
        public IConfigurationSection Configuration { get; private set; }
        public RoutingConfiguration(IConfigurationRoot root)
        {
            Configuration = root.GetSection("routing");
        }
        /// <summary>
        /// Whether a context matches a given route role.
        /// </summary>
        /// <param name="role">The role to check for</param>
        /// <param name="ctx">The context to compare</param>
        /// <returns></returns>
        public bool MatchesForRole(string role, HttpContext ctx)
        {
            StringValues hostname;
            var roleSection = Configuration.GetSection(role);
            var strRegex = roleSection["value"];
            if (string.IsNullOrEmpty(strRegex))
            {
                throw new Exception("Value for route with role is not set!");
            }
            strRegex = strRegex.Replace("\\\\", "\\", StringComparison.CurrentCulture);
            var regex = new Regex(strRegex);

            if (ctx.Request.Headers.TryGetValue("Host", out hostname))
            {
                return regex.IsMatch(hostname); 
            }
            else
            {
                return false;
            }
        }
    }
}