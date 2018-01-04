using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using nvoid.db;
using nvoid.db.Batching;
using nvoid.db.Extensions;
using Netlyt.Service;
using Netlyt.Service.Format;
using Netlyt.Service.Integration;
using Netlyt.Service.IntegrationSource;
using Netlyt.Web.Middleware;
using Netlyt.Web.Middleware.Hmac;
using Netlyt.Web.Services;
using Newtonsoft.Json.Linq; 

namespace Netlyt.Web.Controllers
{
    /// <summary>
    /// TODO: Create a service for the actions performed in this controller.
    /// </summary>
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = Netlyt.Data.AuthenticationSchemes.DataSchemes)]
    [Route("data")]
    public class DataIntegrationController : Controller
    {
        private BehaviourContext _behaviourContext;
        private RemoteDataSource<IntegratedDocument> _documentStore;
        private SocialNetworkApiManager _socNetManager;
        private IntegrationService _integrationService;
        private ApiService _apiService;

        public DataIntegrationController(UserManager<User> userManager,
            IUserStore<User> userStore,
            BehaviourContext behaviourCtx,
            SocialNetworkApiManager socNetManager,
            IntegrationService integrationService,
            ApiService apiService)
        {
            _behaviourContext = behaviourCtx;
            _apiService = apiService;
            //Move both of these 
            _documentStore = typeof(IntegratedDocument).GetDataSource<IntegratedDocument>();
            _socNetManager = socNetManager;
            _integrationService = integrationService;
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

        /// <summary>   (An Action that handles HTTP POST requests) Posts entity data record(s). </summary>
        ///
        /// <remarks>   Vasko, 18-Dec-17. </remarks>
        ///
        /// <returns>   An asynchronous result that yields an ActionResult. </returns>

        [Route("[action]")]
        [HttpPost]
        public async Task<ActionResult> Entity()
        {
            //Dont close the body! 
            var userApiId = HttpContext.Session.GetUserApiId();
            var memSource = InMemorySource.Create(Request.Body, new JsonFormatter());
            var type = (DataIntegration)memSource.GetTypeDefinition();
            type.APIKey = _apiService.GetApi(userApiId);
            StringValues tmpSource;
            _integrationService.SaveType(type);
            if (Request.Headers.TryGetValue("DataSource", out tmpSource)) type.Source = tmpSource.ToString();
            type.SaveType(userApiId);
            //Check if the entity type exists
            var harvester = new Harvester<IntegratedDocument>();
            var destination = (new MongoSink<IntegratedDocument>(userApiId));
            destination.LinkTo(_behaviourContext.GetActionBlock());
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
            var userAppId = HttpContext.Session.GetUserApiId();
            var userApi = _apiService.GetApi(userAppId);
            JToken bodyJson = Request.ReadBodyAsJson();

            //Todo: secure this..
            var userFilter = bodyJson["userIdentifier"].PrefixKeys("Document.");
            var socialNetwork = bodyJson["type"];
            JToken socialNetworkDetails = bodyJson["details"];
            //Set the session social network tokens


            //Todo: do this without relying on mongo, using the document store
            var mongoCollection = _documentStore.AsMongoDbQueryable();
            var apiQuery = Builders<IntegratedDocument>.Filter.Where(x => x.APIId == userApi.Id);
            var userDocumentFilter = BsonSerializer.Deserialize<BsonDocument>(userFilter.ToString());
            //var entityQuery = Builders<IntegratedDocument>.Filter.(userDocumentFilter);
            var query = Builders<IntegratedDocument>.Filter.And(apiQuery, userDocumentFilter);


            //Get the entity document
            var matchingDocument = mongoCollection.Find(query).First();
            if (matchingDocument == null)
            {
                return Json(new { success = false, message = "Document does not exist!" });
            }
            else
            {
                //Set social network token value
                var socToken = ReservedDocumentTokens.GetUserSocialNetworkTokenName(socialNetwork.ToString());
                matchingDocument.Reserved.Set(socToken, socialNetworkDetails["userToken"].ToString());
                //Save the modified entity
                matchingDocument.SaveOrUpdate(x => x.Id == matchingDocument.Id);
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



    }
}
