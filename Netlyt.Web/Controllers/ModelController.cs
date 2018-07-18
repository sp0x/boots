using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Donut;
using Donut.Data;
using Donut.Integration;
using Donut.Orion;
using Newtonsoft.Json.Linq;
using Netlyt.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Data;
using Netlyt.Web.Helpers;
using Netlyt.Web.ViewModels;
using Newtonsoft.Json;
using DataIntegration = Donut.Data.DataIntegration;

namespace Netlyt.Web.Controllers
{
    [Route("model")]
    [Authorize]
    public class ModelController : Controller
    {
        private IOrionContext _orionContext;
        private UserService _userService;
        private IMapper _mapper;
        private ModelService _modelService;
        private IIntegrationService _integrationService;
        private SignInManager<User> _signInManager;
        private IConfiguration _configuration;
        private ManagementDbContext _db;

        public ModelController(IMapper mapper,
            IOrionContext behaviourCtx,
            UserManager<User> userManager,
            UserService userService,
            ModelService modelService,
            IIntegrationService integrationService,
            SignInManager<User> signInManager,
            IConfiguration configuration,
            ManagementDbContext db)
        {
            _mapper = mapper;
            //_modelContext = typeof(Model).GetDataSource<Model>(); 
            _orionContext = behaviourCtx;
            _userService = userService;
            _modelService = modelService;
            _integrationService = integrationService;
            _signInManager = signInManager;
            _configuration = configuration;
            _db = db;
        }

        [HttpGet("/model/mymodels")]
        public async Task<IEnumerable<ModelViewModel>> GetAll([FromQuery] int page)
        {
            var userModels = await _userService.GetMyModels(page);
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

        [HttpGet("/model/{id}", Name = "GetById")]
        [AllowAnonymous]
        public IActionResult GetById(long id)
        {
            var item = _modelService.GetById(id);
            if (item == null)
            {
                return NotFound();
            }
            var fmi = item.DataIntegrations.FirstOrDefault();
            var fIntegration = _db.Integrations
                .Include(x=>x.APIKey)
                .FirstOrDefault(x => x.Id == fmi.IntegrationId);

            var mapped = _mapper.Map<ModelViewModel>(item);
            mapped.ApiKey = fIntegration.APIKey.AppId;
            mapped.ApiSecret = fIntegration.APIKey.AppSecret;
            mapped.Endpoint = $"http://inference.netlyt.com/";
            if (item.Performance != null)
            {
                mapped.Performance.IsRegression = true;
                mapped.Performance.ReportUrl = $"http://api.netlyt.com/model/getAsset?path={mapped.Performance.ReportUrl}";
                mapped.Performance.TestResultsUrl = $"http://api.netlyt.com/model/getAsset?path={mapped.Performance.TestResultsUrl}";
            }
            return new ObjectResult(mapped);
        }

        [HttpGet("/model/getAsset")]
        [AllowAnonymous]
        public IActionResult GetAsset(string path)
        {
            var isAbsolute = false;
            var expPath = OrionSink.GetExperimentsPath(_configuration, out isAbsolute, ref path);
            Trace.WriteLine(expPath);
            Console.WriteLine(expPath);
            var assetPath = System.IO.Path.Combine(expPath, path);
            if (!System.IO.File.Exists(assetPath))
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        [HttpPost("/model")]
        public async Task<IActionResult> Create([FromBody] ModelCreationViewModel item)
        {
            if (item == null)
            {
                return BadRequest();
            }
            var user = await _userService.GetCurrentUser();
            var integration = _userService.GetUserIntegration(user, item.DataSource);
            var relations = item.Relations?.Select(x => new FeatureGenerationRelation(x[0], x[1]));
            var targets = new ModelTargets().AddTarget(integration.GetField(item.TargetAttribute));
            //This really needs a builder..
            var newModel = await _modelService.CreateModel(user,
                item.Name,
                new List<DataIntegration>(new[] { integration }),
                item.Callback,
                item.GenerateFeatures,
                relations,
                targets);
            return CreatedAtRoute("GetById", new { id = newModel.Id }, item);
        }
        [AllowAnonymous]
        [HttpPost("/model/createUser")]
        public async Task<IActionResult> CreateUser()
        {
            var user = await _userService.GetCurrentUser();
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
                var newUserCreated = _userService.CreateUser(newRegistration, out user);
                if (newUserCreated.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                }

            }
            return Json(new { status = true });
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
            var user = await _userService.GetCurrentUser();
            if (!string.IsNullOrEmpty(modelData.UserEmail))
            {
                _userService.SetUserEmail(user, modelData.UserEmail);
            }
            var relations = new List<FeatureGenerationRelation>();
            if (modelData.Target == null || string.IsNullOrEmpty(modelData.Target.Name))
            {
                return BadRequest("Target is required.");
            }
            var integration = _integrationService.GetById(modelData.IntegrationId)
                .Include(x=>x.Fields)
                .Include(x=>x.Models)
                .Include(x=>x.APIKey)
                .FirstOrDefault();
            if (integration == null)
            {
                return NotFound("Integration not found.");
            }
            var modelName = modelData.Name.Replace(".", "_");
            if (modelData.IdColumn != null && !string.IsNullOrEmpty(modelData.IdColumn.Name))
            {
                integration.DataIndexColumn = modelData.IdColumn.Name;
                _db.SaveChanges();
            }
            var targets = new ModelTargets().AddTarget(integration.GetField(modelData.Target.Name));
            var newModel = await _modelService.CreateModel(user,
                modelData.Name,
                new List<DataIntegration>(new[] { integration }),
                "",
                modelData.GenerateFeatures,
                relations,
                targets);
            //If we don`t use features, go straight to training
            if (!newModel.UseFeatures)
            {
                var t_id = await _modelService.TrainModel(newModel, newModel.GetRootIntegration());
            }
            return CreatedAtRoute("GetById", new { id = newModel.Id }, _mapper.Map<ModelViewModel>(newModel));
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
            var user = await _userService.GetCurrentUser();
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
                        _userService.SetUserEmail(user, modelParams.UserEmail);
                    }
                    else
                    {
                        return BadRequest();
                    }
                    result = await _integrationService.CreateOrAppendToIntegration(sourceStream, fileContentType,
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
                var relations = new List<FeatureGenerationRelation>();
                var targets = new ModelTargets().AddTarget(newIntegration.GetField(modelParams.Target));
                var newModel = await _modelService.CreateModel(user,
                    modelParams.Name,
                    new List<IIntegration>(new[] { newIntegration }),
                    "",
                    true,
                    relations,
                    targets);
                return CreatedAtRoute("GetById", new { id = newModel.Id }, _mapper.Map<ModelViewModel>(newModel));
            }
            return null;
        }

        [HttpPost("/model/{id}/infer")]
        public async Task<IActionResult> Infer(long id)
        {
            var model = _modelService.GetById(id);
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
        public IActionResult GetModelPreparationStatus(long modelId)
        {
            var status = _modelService.GetModelPrepStatus(modelId);
            return Json(new { status = status.ToString().ToLower() });
        }

        [HttpGet("/model/{id}/trainingStatus")]
        public IActionResult GetTrainingStatus(long id)
        {
            var trainingTask = _modelService.GetTrainingStatus(id);
            if (trainingTask == null) return NotFound();
            return Json(new {status = trainingTask.Status.ToString().ToLower()});
        }

        [HttpPut("/model/{id}")]
        public IActionResult Update(long id, [FromBody] ModelUpdateViewModel item)
        {
            if (item == null || item.Id != id)
            {
                return BadRequest();
            }

            var model = _modelService.GetById(id);
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
            await _userService.DeleteModel(id);
            return new NoContentResult();
        }

        [HttpPost("/model/{id}/[action]")]
        public async Task<IActionResult> Train(long id)
        {
            var model = _modelService.GetById(id);
            //var json = await Request.GetRawBodyString();
            var trainRequest = OrionQuery.Factory.CreateTrainQuery(model, model.GetRootIntegration());
            //kick off background async task to train
            // return 202 with wait at endpoint
            //async task
            // do training
            // set current model to the id of whatever came back
            var m_id = await _orionContext.Query(trainRequest);
            return Accepted(m_id);
        }

        [HttpPost("/model/{id}/[action]")]
        public IActionResult Deploy(long id)
        {
            var json = Request.GetRawBodyString();
            JObject data = JObject.Parse(json.Result);
            var m = _modelService.GetById(id);
            if (m == null) return NotFound();
            m.CurrentModel = data.GetValue("current_model").ToString();
            _modelService.SaveChanges();
            return new NoContentResult();
        }


    }
}