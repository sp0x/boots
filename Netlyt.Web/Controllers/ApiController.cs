using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using nvoid.db;
using nvoid.db.Extensions;
using nvoid.Integration;
using Netlyt.Web.Middleware;
using Netlyt.Web.Middleware.Hmac;
using Netlyt.Web.Services;
using Newtonsoft.Json.Linq;

namespace Netlyt.Web.Controllers
{
    [Produces("application/json")]
    [Route("api")]
    public class ApiController : Controller
    {
        private SocialNetworkApiManager _socialApiMan;
        private RemoteDataSource<ApiAuth> _apiStore;


        public ApiController(SocialNetworkApiManager socialApiMan)
        {
            _socialApiMan = socialApiMan;
            _apiStore = typeof(ApiAuth).GetDataSource<ApiAuth>();
        }

        [Route("[action]")]
        [HttpPost]
        public async Task<ActionResult> RegisterSocialNetwork()
        {
            var userApiId = HttpContext.Session.GetUserApiId();
            JToken bodyJson = Request.ReadBodyAsJson();
            string socnetType = bodyJson["type"]?.ToString();
            JToken appDetails = bodyJson["details"];
            string appId = appDetails["appId"]?.ToString();
            string appSecret = appDetails["secret"]?.ToString();

            _socialApiMan.RegisterNetwork(HttpContext.Session, socnetType, appId, appSecret);
            return Json(new { success = true });
        }

        /// <summary>
        /// Gets the all permissions of this api
        /// </summary>
        /// <returns></returns>
        [Route("[action]")]
        [HttpGet]
        public async Task<ActionResult> SocialPermissions(string type = "Local")
        {
            var apiId = HttpContext.Session.GetUserApiId();
            var api = _apiStore.First(x => x.Id == apiId);
            ActionResult result = null;
            if (api == null)
            {
                result = Json(new { success = false, message = "Unknown api id!" });
            }
            else
            {
                if (api.Permissions.ContainsKey(type))
                {
                    result = Json(new { success = true, data = api.Permissions[type] });
                }
                else
                {
                    result = Json(new { success = true, data = new string[] { } });
                }

            }
            return result;
        }
    }
}
