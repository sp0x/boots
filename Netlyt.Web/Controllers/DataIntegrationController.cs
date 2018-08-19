using System.IO;
using System.Threading.Tasks;
using Donut;
using Donut.Orion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using nvoid.db;
using nvoid.db.Extensions;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.Web.Middleware;
using Netlyt.Web.Middleware.Hmac;
using Newtonsoft.Json.Linq; 

namespace Netlyt.Web.Controllers
{
    /// <summary>
    /// To be deprecated..
    /// </summary>
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = Netlyt.Data.AuthenticationSchemes.ApiSchemes)]
    [Route("data")]
    public class DataIntegrationController : Controller
    { 
        private RemoteDataSource<IntegratedDocument> _documentStore; 
        private ApiService _apiService;
        private IIntegrationService _integrationService;

        public DataIntegrationController(UserManager<User> userManager,
            IUserStore<User> userStore,
            IOrionContext behaviourCtx,
            ManagementDbContext context,
            SocialNetworkApiManager socNetManager,
            IIntegrationService integrationService,
            ApiService apiService)
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
            var result = await _integrationService.CreateOrAppendToIntegration(Request.Body, null);
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
                var socToken = ReservedDocumentTokensService.GetUserSocialNetworkTokenName(socialNetwork.ToString());
                matchingDocument.Reserved.Set(socToken, socialNetworkDetails["userToken"].ToString());
                //Save the modified entity
                matchingDocument.SaveOrUpdate(x => x.Id == matchingDocument.Id);
                return Json(new { success = true });
            }
        }

    }
}
