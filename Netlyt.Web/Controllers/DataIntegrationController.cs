using System.IO; 
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Primitives;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using nvoid.db;
using nvoid.db.Batching;
using nvoid.db.Extensions;
using Netlyt.Service;
using Netlyt.Service.Data;
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
    [Authorize(AuthenticationSchemes = Netlyt.Data.AuthenticationSchemes.ApiSchemes)]
    [Route("data")]
    public class DataIntegrationController : Controller
    { 
        private RemoteDataSource<IntegratedDocument> _documentStore; 
        private ApiService _apiService;
        private IntegrationService _integrationService;

        public DataIntegrationController(UserManager<User> userManager,
            IUserStore<User> userStore,
            OrionContext behaviourCtx,
            ManagementDbContext context,
            SocialNetworkApiManager socNetManager,
            IntegrationService integrationService,
            ApiService apiService,
            IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
        {  
            _apiService = apiService; 
            _documentStore = typeof(IntegratedDocument).GetDataSource<IntegratedDocument>();
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
            //GetRoutes();
            var result = await _integrationService.CreateOrFillIntegration(Request.Body, null);
            return Json(new
            {
                success = true
            });
        }

        [Route("[action]")]
        [HttpPost]
        public ActionResult SocialEntity()
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
