using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netlyt.Service;
using Netlyt.Web.Middleware;
using Netlyt.Web.Middleware.Hmac;
using Netlyt.Web.Services;
using Newtonsoft.Json.Linq;

namespace Netlyt.Web.Controllers
{
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = Netlyt.Data.AuthenticationSchemes.ApiSchemes)]
    [Route("social")]
    public class SocialController : Controller
    {
        private SocialNetworkApiManager _socialApiMan; 
        private ApiService _apiService;


        public SocialController(SocialNetworkApiManager socialApiMan,
            ApiService apiService)
        {
            _socialApiMan = socialApiMan;
            _apiService = apiService;
        }

        [Route("api/[action]")]
        [HttpPost]
        public ActionResult RegisterSocialNetwork()
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
        [Route("Permissions")]
        [AllowAnonymous]
        public ActionResult Permissions(string type = "Local")
        {
            var apiId = HttpContext.Session.GetUserApiId();
            var api = _apiService.GetApi(apiId);
            ActionResult result = null;
            if (api == null)
            {
                result = Json(new { success = false, message = "Unknown api id!" });
            }
            else
            {
                var typePermission = api.Permissions.FirstOrDefault(x => x.Type == type);
                if (typePermission!=null)
                {
                    result = Json(new { success = true, data = typePermission });
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
