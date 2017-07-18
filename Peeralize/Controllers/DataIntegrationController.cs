using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.MongoDB;
using Microsoft.AspNetCore.Mvc;
using nvoid.Integration;
using Newtonsoft.Json;
using Peeralize.Middleware.Hmac;
using Peeralize.Service;
using Peeralize.Service.Auth;
using Peeralize.Service.IntegrationSource;
using Peeralize.Service.Source;

namespace Peeralize.Controllers
{ 
    [Produces("application/json")]
    [Authorize(ActiveAuthenticationSchemes = "Hmac")]
    [Route("data")]
    public class DataIntegrationController : Controller
    {
        private UserManager<ApplicationUser>  _userManager;
        private IUserStore<ApplicationUser> _userStore;
        public DataIntegrationController(UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore)
        {
            _userManager = userManager;
            _userStore = userStore;
        }


        [Route("GetStatus")]
        [HttpGet]
        public ActionResult GetStatus()
        {
            return Json(new
            {
                success = true
            });
        }

        [Route("[action]")]
        [HttpPost]
        public ActionResult Entity()
        {
            //Dont close the body! 
            var reader = new StreamReader(Request.Body); 
            var userApiId = HttpContext.Session.GetApiUserId();
            var fs = InMemorySource.Create(Request.Body, new JsonFormatter());
            var type = fs.GetTypeDefinition();
            type.UserId = userApiId;
            var harvester = new Harvester();
            harvester.SetDestination(new MongoSink(userApiId));
            harvester.AddType(type, fs);
            harvester.Synchronize();
            
            return Json(new
            {
                success = true,
                typeId = type.Id
            });
        }

        [Route("[action]")]
        [HttpPost]
        public ActionResult EntityData()
        {

            var requestBody = (new StreamReader(Request.Body)).ReadToEnd();
            return Json(new
            {
                success = true,
                id = 2
            });
        }

    }
}