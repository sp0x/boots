using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Donut;
using Donut.Data;
using Donut.Integration;
using Donut.Models;
using Donut.Orion;
using Newtonsoft.Json.Linq;
using Netlyt.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Netlyt.Data.ViewModels;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Cloud;
using Netlyt.Service.Data;
using Netlyt.Service.Helpers;
using Netlyt.Web.Extensions;
using Newtonsoft.Json;
using DataIntegration = Donut.Data.DataIntegration;

namespace Netlyt.Web.Controllers
{
    [Route("model")]
    [Authorize]
    public class ModelController : Controller
    {
        private IOrionContext _orionContext;
        private IUserManagementService _userManagementService;
        private IMapper _mapper;
        private ModelService _modelService;
        private IIntegrationService _integrationService;
        private SignInManager<User> _signInManager;
        private IConfiguration _configuration;
        private ManagementDbContext _db;
        private UserManager<User> _userManager;
        private INotificationService _notifications;

        public ModelController(IMapper mapper,
            IOrionContext behaviourCtx,
            UserManager<User> userManager,
            IUserManagementService userManagementService,
            ModelService modelService,
            IIntegrationService integrationService,
            SignInManager<User> signInManager,
            IConfiguration configuration,
            ManagementDbContext db,
            INotificationService notifications)
        {
            _notifications = notifications;
            _mapper = mapper;
            //_modelContext = typeof(Model).GetDataSource<Model>(); 
            _orionContext = behaviourCtx;
            _userManagementService = userManagementService;
            _modelService = modelService;
            _integrationService = integrationService;
            _signInManager = signInManager;
            _userManager = userManager;
            _configuration = configuration;
            _db = db;
        }

        [HttpGet("/model/mymodels/{type}")]
        public async Task<IEnumerable<ModelViewModel>> GetAll([FromQuery] int page, string type)
        {
            var user = await _userManagementService.GetCurrentUser();
            var userModels = _modelService.GetAllForUser(user, page, 200);
            if (type == "building")
            {
                userModels = userModels.Where(x =>
                    x.TrainingTasks.Any(tt => tt.Status == TrainingTaskStatus.InProgress ||
                                              tt.Status == TrainingTaskStatus.Starting));
            }
            var viewModels = userModels.Select(m => _mapper.Map<ModelViewModel>(m));
            return viewModels;
        }

        [HttpGet("/model/paramlist")]
        public async Task<JsonResult> GetParamsList()
        {
            var orionQuery = new OrionQuery(OrionOp.ParamList);
            var param = await _orionContext.Query(orionQuery);
            return Json(param);
        }

        [HttpGet("/model/classlist")]
        public async Task<JsonResult> GetClassList()
        {
            var query = new OrionQuery(OrionOp.GetModelList);
            var param = await _orionContext.Query(query);
            return Json(param);
        }

        [HttpGet("/model/{id}/performance", Name = "GetModelPerformance")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPerformance(long id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            var item = _modelService.GetById(id, user);
            if (item == null) return NotFound();
            var status = _modelService.GetModelStatus(item);
            if (!item.Permissions.Any(x=>x.ShareWith.Id == user.Organization.Id)){
                return Forbid("You are not authorized to view this model");
            }
            if (status != Donut.Models.ModelPrepStatus.Done)
            {
                var resp = Json(new {message = "Try again later.", status = status.ToString().ToLower()});
                resp.StatusCode = 204;
                return resp;
            }

            if (status == Donut.Models.ModelPrepStatus.Incomplete)
            {
                var resp = Json(new { message = "Not built yet.", status = status.ToString().ToLower() });
                resp.StatusCode = 204;
                return resp;
            }

            var fmi = item.DataIntegrations.FirstOrDefault();
            var fIntegration = _db.Integrations
                .Include(x => x.APIKey)
                .FirstOrDefault(x => x.Id == fmi.IntegrationId); 
            var mapped = _mapper.Map<ModelViewModel>(item); 
            
            mapped.ApiKey = fIntegration.APIKey.AppId;
            mapped.ApiSecret = fIntegration.APIKey.AppSecret;
            return Json(mapped);
        }

        [HttpGet("/model/{id}", Name = "GetById")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(long id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            var item = _modelService.GetById(id, user);
            if (item == null) return NotFound();
            if (!item.Permissions.Any(x=>x.ShareWith.Id == user.Organization.Id)){
                return Forbid("You are not authorized to view this model");
            }
            var fmi = item.DataIntegrations.FirstOrDefault();
            var fIntegration = _db.Integrations
                .Include(x=>x.APIKey)
                .FirstOrDefault(x => x.Id == fmi.IntegrationId);

            var mapped = _mapper.Map<ModelViewModel>(item);
            mapped.ApiKey = fIntegration.APIKey.AppId;
            mapped.ApiSecret = fIntegration.APIKey.AppSecret;
            return new ObjectResult(mapped);
        }

        [HttpGet("/model/getAsset")]
        [AllowAnonymous]
        public IActionResult GetAsset(string path)
        {
            var assetPath = _orionContext.GetExperimentAsset(path);
            if (assetPath==null)
            {
                return NotFound();
            }
            else
            {
                var assetName = Path.GetFileName(assetPath);
                var bytes = System.IO.File.Open(assetPath, FileMode.Open);
                return File(bytes, "application/force-download", assetName);
            }
        }

        [AllowAnonymous]
        [HttpPost("/model/createUser")]
        public async Task<IActionResult> CreateUser()
        {
            var user = await _userManagementService.GetCurrentUser();
            var rnd = new Random();
            if (user == null)
            {
                var newRegistration = new Service.Models.Account.RegisterViewModel();
                newRegistration.Password = "ComplexP4ssword!";
                var randomInt = rnd.Next(100000, 9000000);
                newRegistration.Email = "somemail" + randomInt + "@mail.com";
                newRegistration.FirstName = "Userx";
                newRegistration.LastName = "Lastnamex";
                newRegistration.Org = "Lol";
                var result = await _userManagementService.CreateUser(newRegistration);
                if (result.Item1.Succeeded)
                {
                    await _signInManager.SignInAsync(result.Item2, isPersistent: false);
                }

            }
            return Json(new { status = true });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        [HttpPost("/model")]
        public async Task<IActionResult> Create([FromBody] ModelCreationViewModel item)
        {
            if (item == null) return BadRequest();
            var user = await _userManagementService.GetCurrentUser();
            var integration = _integrationService.GetUserIntegration(user, item.DataSource);
            if (!integration.Permissions.Any(x=>x.ShareWith.Id == user.Organization.Id)){
                return Forbid("You are not authorized to use this integration for model building");
            }
            var relations = item.Relations?.Select(x => new FeatureGenerationRelation(x[0], x[1]));
            var targets = new ModelTarget(integration.GetField(item.TargetAttribute));
            //This really needs a builder..
            var newModel = await _modelService.CreateModel(user,
                item.Name,
                new List<DataIntegration>(new[] { integration }),
                item.Callback,
                item.GenerateFeatures,
                relations,
                null,
                targets);
            return CreatedAtRoute("GetById", new { id = newModel.Id }, item);
        }

        /// <summary>
        /// TODO: Clean these create methods once we dont have to support multiple UIs..
        /// </summary>
        /// <param name="props"></param>
        /// <returns></returns>
        [HttpPost("/model/createEmpty")]
        public async Task<IActionResult> CreateEmpty([FromBody] CreateEmptyModelViewModel props)
        {
            if (props == null) return BadRequest();
            if (string.IsNullOrEmpty(props.ModelName)) return BadRequest("Model name is required.");
            if (props.Targets == null) return BadRequest("Model targets are required.");
            try
            {
                var user = await _userManagementService.GetCurrentUser();
                Model newModel = await _modelService.CreateEmptyModel(user, props);
                return CreatedAtRoute("GetById", new {id = newModel.Id}, _mapper.Map<ModelViewModel>(newModel));
            }
            catch (Forbidden f)
            {
                return Forbid(f.Message);
            }
            catch (NotFound nf)
            {
                return NotFound(nf.Message);
            }
        }

        /// <summary>
        /// Automatically creates a new model with generated features and model parameters
        /// </summary>
        /// <returns></returns>
        [HttpPost("/model/createAuto")]
        //[AllowAnonymous]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> CreateAuto([FromBody]CreateAutomaticModelViewModel modelData)
        {
            var user = await _userManagementService.GetCurrentUser();
            if (!string.IsNullOrEmpty(modelData.UserEmail))
            {
                _userManagementService.SetUserEmail(user, modelData.UserEmail);
            }
            if (modelData.Target == null || string.IsNullOrEmpty(modelData.Target.Name))
            {
                return BadRequest("Target is required.");
            }
            DataIntegration integration = await _integrationService.GetIntegrationForAutobuild(modelData);
            var targets = new ModelTarget(integration.GetField(modelData.Target.Name));
            var modelName = modelData.Name.Replace(".", "_");
            var newModel = await _modelService.CreateModel(user,
                modelName,
                new List<DataIntegration>(new[] { integration }),
                "",
                modelData.GenerateFeatures,
                null,
                null,
                targets);
            //If we don`t use features, go straight to training
            if (!newModel.UseFeatures)
            {
                var t_id = await _modelService.TrainModel(newModel, newModel.GetRootIntegration());
            }
            return CreatedAtRoute("GetById", new { id = newModel.Id }, _mapper.Map<ModelViewModel>(newModel));
        }

        /// <summary>
        /// Save the scripts and run the model..
        /// </summary>
        /// <param name="data"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost("/model/{id}/executeScript")]
        public async Task<IActionResult> ExecuteScript([FromBody] ScriptViewModel data, long id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            var model = _modelService.GetById(id, user);
            if (model == null) return NotFound();
            JToken trainingTask = null;
            if (data?.Data!=null && data.Data.UseScript)
            {
                trainingTask = await _modelService.TrainModel(model, model.GetRootIntegration(), 
                    new TrainingScript(data.Data.Script, data.Data.Code));
            }
            else
            {
                trainingTask = await _modelService.TrainModel(model, model.GetRootIntegration());
            }
            return Json(new { models_tasks = trainingTask, success= true });
        }

        [HttpPost("/model/{id}/build")]
        public async Task<IActionResult> Build(long id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            var model = _modelService.GetById(id, user);
            if (model == null) return NotFound();
            
            var trainingTask = await _modelService.TrainModel(model, model.GetRootIntegration());
            _notifications.SendModelBuilding(model, trainingTask);
            return Json(new {taskId = trainingTask});
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Attributes.DisableFormValueModelBinding]
        [RequestSizeLimit(100_000_000)]
        [HttpPost("/model/integrate")]
        public async Task<IActionResult> CreateAndIntegrate()
        {
            IIntegration newIntegration = null;
            NewModelIntegrationViewmodel modelParams = new NewModelIntegrationViewmodel();
            string targetFilePath = Path.GetTempFileName();
            string fileContentType = null;
            var user = await _userManagementService.GetCurrentUser();
            var apiKey = await _userManagementService.GetCurrentApi();
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
            }
            DataImportResult result = null;
            using (var sourceStream = System.IO.File.Create(targetFilePath))
            {
                var form = await Request.StreamFile(sourceStream);
                fileContentType = form.GetValue("mime-type").ToString();
                var valueProviderResult = form.GetValue("modelData");
                if (valueProviderResult == null) return BadRequest("No integration given.");
                modelParams = JsonConvert.DeserializeObject<NewModelIntegrationViewmodel>(valueProviderResult.ToString());
                sourceStream.Position = 0;
                try
                {
                    if (!string.IsNullOrEmpty(modelParams.UserEmail))
                    {
                        _userManagementService.SetUserEmail(user, modelParams.UserEmail);
                    }
                    else
                    {
                        return BadRequest();
                    }
                    result = await _integrationService.CreateOrAppendToIntegration(user, apiKey, sourceStream, fileContentType,
                        modelParams.Name);
                    newIntegration = result?.Integration;
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
            if (result != null)
            {
                System.IO.File.Delete(targetFilePath);
                var targets = new ModelTarget(newIntegration.GetField(modelParams.Target));
                var newModel = await _modelService.CreateModel(user,
                    modelParams.Name,
                    new List<IIntegration>(new[] { newIntegration }),
                    "",
                    true,
                    null,
                    null,
                    targets);
                return CreatedAtRoute("GetById", new { id = newModel.Id }, _mapper.Map<ModelViewModel>(newModel));
            }
            return null;
        }

        [HttpPost("/model/{id}/infer")]
        public async Task<IActionResult> Infer(long id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            var model = _modelService.GetById(id, user);
            if (model == null) return NotFound();
            var rootIgn = model.GetRootIntegration();
            //TODO:
            //Integrate the input to the root integration(DataImportTask)
            //Run feature extraction
            //Ask orion to make a prediction on the selected feature


            var trainRequest = OrionQuery.Factory.CreatePredictionQuery(model, model.GetRootIntegration());
            //kick off background async task to train
            // return 202 with wait at endpoint
            //async task
            // do training
            // set current model to the id of whatever came back
            var m_id = await _orionContext.Query(trainRequest);
            return Accepted(m_id);
        }

        [HttpGet("/model/{modelId}/modelPrepStatus")]
        public async Task<IActionResult> GetModelPreparationStatus(long modelId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            var model = _modelService.GetById(modelId, user);
            if (model == null) return NotFound();
            var status = _modelService.GetModelStatus(model);
            return Json(new { status = status.ToString().ToLower() });
        }

        [HttpGet("/model/{id}/trainingStatus")]
        public IActionResult GetTrainingStatus(long id)
        {
            var trainingTask = _modelService.GetTrainingStatus(id);
            if (trainingTask == null) return NotFound();
            return Json(new {status = trainingTask.Status.ToString().ToLower()});
        }

        [HttpGet("/model/{id}/status")]
        public async Task<IActionResult> GetStatus(long id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            var model = _modelService.GetById(id, user);
            if (model == null) return NotFound();
            var status = _modelService.GetModelStatus(model);
            return Json(new { status = status.ToString().ToLower() });
        }

        [HttpPut("/model/{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] ModelUpdateViewModel item)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            if (item == null || item.Id != id)
            {
                return BadRequest();
            }

            var model = _modelService.GetById(id, user);
            if (model == null)
            {
                return NotFound();
            }

            //TODO: add logic to check if we're updating integrations or what
            //_modelService.UpdateModel(model, item);
            // _modelContext.Save();
            return new NoContentResult();
        }

        [HttpDelete("/model/{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var user = await _userManagementService.GetCurrentUser();
            _modelService.DeleteModel(user, id);
            return new NoContentResult();
        }

        [HttpPost("/model/{id}/[action]")]
        public async Task<IActionResult> Train(long id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            var model = _modelService.GetById(id, user);
            if (model == null) return NotFound();
            //var json = await Request.GetRawBodyString();
            var m_id = await _modelService.TrainModel(model, model.GetRootIntegration());
            return Accepted(m_id);
        }

        [HttpPost("/model/{id}/[action]")]
        public async Task<IActionResult> Deploy(long id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            throw new NotImplementedException();
            var json = Request.GetRawBodyString();
            JObject data = JObject.Parse(json.Result);
            var m = _modelService.GetById(id, user);
            if (m == null) return NotFound();
            m.CurrentModel = data.GetValue("current_model").ToString();
//            _modelService.SaveChanges();
            return new NoContentResult();
        }


    }
}