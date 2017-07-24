using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.MongoDB;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using nvoid.db.DB.RDS;
using nvoid.db.Extensions;
using nvoid.Integration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Peeralize.Middleware;
using Peeralize.Middleware.Hmac;
using Peeralize.Service;
using Peeralize.Service.Auth;
using Peeralize.Service.Integration;
using Peeralize.Service.IntegrationSource;
using Peeralize.Service.Source;
using Peeralize.Services;

namespace Peeralize.Controllers
{
    /// <summary>
    /// TODO: Create a service for the actions performed in this controller.
    /// </summary>
    [Produces("application/json")]
    [Authorize(ActiveAuthenticationSchemes = "Hmac")]
    [Route("data")]
    public class DataIntegrationController : Controller
    {
        private UserManager<ApplicationUser>  _userManager;
        private IUserStore<ApplicationUser> _userStore;
        private RemoteDataSource<IntegrationTypeDefinition> _typeStore;
        private BehaviourContext _behaviourContext;
        private RemoteDataSource<IntegratedDocument> _documentStore;
        private SocialNetworkApiManager _socNetManager;

        public DataIntegrationController(UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            BehaviourContext behaviourCtx,
            SocialNetworkApiManager socNetManager)
        {
            _userManager = userManager;
            _userStore = userStore;
            _behaviourContext = behaviourCtx;
            //Move both of these
            _typeStore = typeof(IntegrationTypeDefinition).GetDataSource<IntegrationTypeDefinition>();
            _documentStore = typeof(IntegratedDocument).GetDataSource<IntegratedDocument>();
            _socNetManager = socNetManager;

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
            var userApiId = HttpContext.Session.GetUserApiId();
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
        public async Task<ActionResult> SocialEntity()
        {
            var userApiId = HttpContext.Session.GetUserApiId(); 
            JToken bodyJson = Request.ReadBodyAsJson();

            //Todo: secure this..
            var userFilter = bodyJson["userIdentifier"].PrefixKeys("Document.");
            var socialNetwork = bodyJson["type"];
            JToken socialNetworkDetails = bodyJson["details"];
            //Set the session social network tokens


            //Todo: do this without relying on mongo, using the document store
            var mongoCollection = _documentStore.MongoDb();
            var apiQuery = Query<IntegratedDocument>.Where(x => x.UserId == userApiId);
            var userDocumentFilter = BsonSerializer.Deserialize<BsonDocument>(userFilter.ToString());
            var entityQuery = new QueryDocument(userDocumentFilter);
            var query = Query.And(apiQuery, entityQuery);


            //Get the entity document
            var matchingDocument = mongoCollection.FindOne(query);
            if (matchingDocument == null)
            {
                return Json(new {success = false, message = "Document does not exist!"});
            }
            else
            {
                //Set social network token value
                var socToken = Services.ReservedDocumentTokens.GetUserSocialNetworkTokenName(socialNetwork.ToString());
                matchingDocument.Reserved.Set(socToken, socialNetworkDetails["userToken"].ToString() );
                //Save the modified entity
                matchingDocument.SaveOrUpdate<IntegratedDocument>(x=> x.Id == matchingDocument.Id); 
                return Json(new { success = true });
            }
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
            var integrationTypeDefinitions = _typeStore.Where(x => x.UserId == apiId && x.Fields == type.Fields);
            if (integrationTypeDefinitions == null || integrationTypeDefinitions.Count() == 0)
            {
                existingDefinition = null;
                return false;
            }
            existingDefinition = integrationTypeDefinitions.First();
            return existingDefinition != null;
        }

    }
}