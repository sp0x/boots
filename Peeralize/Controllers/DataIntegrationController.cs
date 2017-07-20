using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.MongoDB;
using Microsoft.AspNetCore.Mvc;
using nvoid.db.DB.RDS;
using nvoid.db.Extensions;
using nvoid.Integration;
using Newtonsoft.Json;
using Peeralize.Middleware.Hmac;
using Peeralize.Service;
using Peeralize.Service.Auth;
using Peeralize.Service.Integration;
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
        private RemoteDataSource<IntegrationTypeDefinition> _typeStore;
        private BehaviourContext _behaviourContext;

        public DataIntegrationController(UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            BehaviourContext behaviourCtx)
        {
            _userManager = userManager;
            _userStore = userStore;
            _typeStore = typeof(IntegrationTypeDefinition).GetDataSource<IntegrationTypeDefinition>();
            _behaviourContext = behaviourCtx;
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
        public async Task<ActionResult> Entity()
        {
            //Dont close the body! 
            var userApiId = HttpContext.Session.GetApiUserId();
            var memSource = InMemorySource.Create(Request.Body, new JsonFormatter());
            var type = (IntegrationTypeDefinition)memSource.GetTypeDefinition();
            IntegrationTypeDefinition oldTypeDef;
            type.UserId = userApiId;
            if (!TypeExists(type, userApiId, out oldTypeDef))
            {
                type.Save();
            }
            else
            {
                type.Id = oldTypeDef.Id;
            }
            //Check if the entity type exists
            var harvester = new Harvester();
            var destination = (new MongoSink(userApiId)).LinkTo(_behaviourContext.GetActionBlock());
            harvester.SetDestination(destination);
            harvester.AddType(type, memSource);
            harvester.Synchronize();
            
            return Json(new
            {
                success = true,
                typeId = type.Id
            });
        }

        [Route("[action]")]
        [HttpPost]
        public async Task<ActionResult> EntityData()
        {

            var requestBody = (new StreamReader(Request.Body)).ReadToEnd();
            return Json(new
            {
                success = true,
                id = 2
            });
        }


        private bool TypeExists(IntegrationTypeDefinition type, string apiId, out IntegrationTypeDefinition existingDefinition)
        {
            existingDefinition = _typeStore.Where(x => x.UserId == apiId && x.Fields == type.Fields).First();
            return existingDefinition != null;
        }
    }
}