using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using nvoid.db.Extensions;
using Newtonsoft.Json.Linq;
using Netlyt.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using nvoid.db;
using Netlyt.Service.Integration;
using Netlyt.Service.Integration.Import;
using Netlyt.Service.Ml;
using Netlyt.Service.Orion;
using Netlyt.Web.Helpers;
using Netlyt.Web.ViewModels;
using Newtonsoft.Json;

namespace Netlyt.Web.Controllers
{
    [Route("model")]
    [Authorize]
    public class ModelController : Controller
    {
        private OrionContext _orionContext;
        private UserService _userService;
        private IMapper _mapper;
        private ModelService _modelService;
        private IntegrationService _integrationService;
        private SignInManager<User> _signInManager;

        public ModelController(IMapper mapper,
            OrionContext behaviourCtx,
            UserManager<User> userManager,
            UserService userService,
            ModelService modelService,
            IntegrationService integrationService,
            SignInManager<User> signInManager)
        {
            _mapper = mapper;
            //_modelContext = typeof(Model).GetDataSource<Model>(); 
            _orionContext = behaviourCtx;
            _userService = userService;
            _modelService = modelService;
            _integrationService = integrationService;
            _signInManager = signInManager;
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
        public IActionResult GetById(long id)
        {
            var item = _modelService.GetById(id);
            if (item == null)
            {
                return NotFound();
            }
            return new ObjectResult(_mapper.Map<ModelViewModel>(item));
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
            //This really needs a builder..
            var newModel = await _modelService.CreateModel(user,
                item.Name,
                new List<DataIntegration>(new[] { integration }),
                item.Callback,
                item.GenerateFeatures,
                relations,
                item.TargetAttribute);
            return CreatedAtRoute("GetById", new { id = newModel.Id }, item);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Attributes.DisableFormValueModelBinding]
        [RequestSizeLimit(100_000_000)]
        [AllowAnonymous]
        [HttpPost("/model/integrate")]
        public async Task<IActionResult> CreateAndIntegrate()
        {
            DataIntegration newIntegration = null;
            NewModelIntegrationViewmodel modelParams = new NewModelIntegrationViewmodel();
            string targetFilePath = Path.GetTempFileName();
            string fileContentType = null;
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
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
            }
            DataImportResult result = null;
            using (var targetStream = System.IO.File.Create(targetFilePath))
            {
                var form = await Request.StreamFile(targetStream);
                fileContentType = form.GetValue("mime-type").ToString();
                var valueProviderResult = form.GetValue("modelData");
                if (valueProviderResult == null) return BadRequest("No integration given.");
                modelParams = JsonConvert.DeserializeObject<NewModelIntegrationViewmodel>(valueProviderResult.ToString());
                targetStream.Position = 0;
                try
                {
                    result = await _integrationService.CreateOrFillIntegration(targetStream, fileContentType,
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
                var newModel = await _modelService.CreateModel(user,
                    modelParams.Name,
                    new List<DataIntegration>(new[] { newIntegration }),
                    "",
                    true,
                    relations,
                    modelParams.Target);
                return CreatedAtRoute("GetById", new { id = newModel.Id }, _mapper.Map<ModelViewModel>(newModel));
            }
            return null;
        }

        [HttpGet("/model/{id}/featureGenerationStatus")]
        public IActionResult GetFeatureGenerationStatus(long id)
        {
            var generationTask = _modelService.GetFeatureGenerationTask(id);
            if (generationTask == null) return NotFound();
            return Json(new { status = generationTask.Status.ToString().ToLower() });
        }

        [HttpGet("/model/id/trainingStatus")]
        public IActionResult GetTrainingStatus(long id)
        {
            return null;
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
            var json = await Request.GetRawBodyString();
            JObject query = JObject.Parse(json);
            //kick off background async task to train
            // return 202 with wait at endpoint
            //async task
            // do training
            // set current model to the id of whatever came back
            var m_id = await _orionContext.Query(query);
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