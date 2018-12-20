using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netlyt.Service;
using Netlyt.Web.Models;
using System.Threading.Tasks; 
using System.IO;
using AutoMapper;
using Donut;
using Donut.Data;
using Donut.Integration;
using Donut.Models;
using Donut.Orion;
using static Netlyt.Web.Attributes;
using Netlyt.Data.ViewModels;
using Netlyt.Service.Cloud;
using Netlyt.Service.Helpers;
using Newtonsoft.Json;

namespace Netlyt.Web.Controllers
{
    [Route("integration")]
    public class IntegrationsController : Controller
    {
        private IIntegrationService _integrationService;
        private IUserManagementService _userManagementService;
        private IMapper _mapper;
        private ModelService _modelService;

        private IOrionContext _orionContext;
        private INotificationService _notifications;
        private ICloudNodeService _nodeService;

        // GET: /<controller>/
        public IntegrationsController(
            IUserManagementService userManagementService,
            IIntegrationService integrationService,
            IMapper mapper,
            IOrionContext orionContext,
            INotificationService notificationsService,
            ICloudNodeService nodeService,
            ModelService modelService)
        {
            _userManagementService = userManagementService;
            _integrationService = integrationService;
            _mapper = mapper;
            _orionContext = orionContext;
            _notifications = notificationsService;
            _nodeService = nodeService;
            _modelService = modelService;
        }

        [HttpGet("/integrations/me")]
        [Authorize]
        public async Task<IEnumerable<DataIntegrationViewModel>> GetAll([FromQuery] int page)
        {
            var user = await _userManagementService.GetCurrentUser();
            int pageSize = 25;
            var dataIntegrations = await _integrationService.GetIntegrations(user, page, pageSize);
            return dataIntegrations.Select(x => _mapper.Map<DataIntegrationViewModel>(x));
        }

        [HttpGet("/integrations/{userId}")]
        [Authorize]
        public async Task<IEnumerable<DataIntegrationViewModel>> GetAll(string userId, [FromQuery] int page)
        {
            int pageSize = 25;
            var user = await _userManagementService.GetCurrentUser();
            var dataIntegrations = await _integrationService.GetIntegrations(user, userId, page, pageSize);
            return dataIntegrations.Select(x => _mapper.Map<DataIntegrationViewModel>(x));
        }

        [HttpGet("/job/{id}")]
        [Authorize]
        public IActionResult GetJob(long id)
        {
            return Accepted();
        }

        [HttpPost("/integration")]
        [Authorize]
        public async Task<IActionResult> CreateIntegration([FromBody]NewIntegrationViewModel integration)
        {
            if (string.IsNullOrEmpty(integration.Name))
            {
                return BadRequest("No integration name given.");
            }
            var user = await _userManagementService.GetCurrentUser();
            var apiKey = await _userManagementService.GetCurrentApi();
            var newIntegration = await _integrationService.Create(user, apiKey, integration.Name, integration.DataFormatType);
            return CreatedAtRoute("GetIntegration", new { id = newIntegration.Id }, _mapper.Map<DataIntegrationViewModel>(newIntegration));
        }



        [HttpPost("/integration/file")]
        [DisableFormValueModelBinding]
        [RequestSizeLimit(100_000_000)]
        [Authorize]
        public async Task<IActionResult> CreateFileIntegration()
        {
            IIntegration newIntegration = null;
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
            }
            NewIntegrationViewModel integrationParams = new NewIntegrationViewModel();
            string targetFilePath = Path.GetTempFileName();
            string fileContentType = null;
            DataImportResult result = null;
            var user = await _userManagementService.GetCurrentUser();
            var apiKey = await _userManagementService.GetCurrentApi();
            using (var targetStream = System.IO.File.Create(targetFilePath))
            {
                var form = await Request.StreamFile(targetStream);
                fileContentType = form.GetValue("mime-type").ToString();
                var valueProviderResult = form.GetValue("integration");
                if (valueProviderResult == null) return BadRequest("No integration given.");
                integrationParams = JsonConvert.DeserializeObject<NewIntegrationViewModel>(valueProviderResult.ToString());
                targetStream.Position = 0;
                try
                {
                    result = await _integrationService.CreateOrAppendToIntegration(targetStream, apiKey, user, fileContentType,
                        integrationParams.Name);
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
                return CreatedAtRoute("GetIntegration", new { id = newIntegration.Id },
                    _mapper.Map<DataIntegrationViewModel>(newIntegration));
            }
            return null;
        }

        [HttpPut("{id}")]
        [Authorize]
        public IActionResult Update(long id, [FromBody] DataIntegrationUpdateViewModel item)
        {
            if (item == null || item.Id != id)
            {
                return BadRequest();
            }
            var integration = _integrationService.GetById(id);
            if (integration == null)
            {
                return NotFound();
            }
            //TODO: add logic to check if we're updating integrations or what

            //_integrationContext.Update(integration);
            // _integrationContext.Save();
            return new NoContentResult();
        }

        [HttpDelete("{id}")]
        [Authorize] 
        public IActionResult Delete(long id)
        {
            var item = _integrationService.GetById(id);
            if (item == null)
            {
                return NotFound();
            }
            _integrationService.Remove(item);
            return new NoContentResult();
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetIntegration(long id)
        {
            try
            {
                var user = await _userManagementService.GetCurrentUser();
                var integrationView = await _integrationService.GetIntegrationView(user, id);
                _notifications.SendIntegrationViewed(id, user.Id);
                return Json(integrationView);
            }
            catch (Forbidden f)
            {
                return Forbid(f.Message);
            }
            catch (NotFound)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Gets just the schema of an integration.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}/schema")]
        public async Task<IActionResult> GetSchema(long id)
        {
            try
            {
                var user = await _userManagementService.GetCurrentUser();
                var schema = await _integrationService.GetSchema(user, id);
                _notifications.SendIntegrationViewed(id, user.Id);
                return Json(schema);
            }
            catch (Forbidden f)
            {
                return Forbid(f.Message);
            }
            catch (NotFound)
            {
                return NotFound();
            }
        }

        


        [HttpPost("/integration/schema")]
        public async Task<IActionResult> UploadAndGetSchema()
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
            }
            try
            {
                var user = await _userManagementService.GetCurrentUser();
                var apiKey = await _userManagementService.GetCurrentApi();
                var result = await _integrationService.CreateOrAppendToIntegration(user, apiKey, Request);
                if (result != null)
                {
                    DataIntegration newIntegration = result.Integration as DataIntegration;
                    DataIntegration integrationWithDescription = await _integrationService.ResolveDescription(user, newIntegration);
                    CreateEmptyModelViewModel modelProps = new CreateEmptyModelViewModel();
                    modelProps.IntegrationId = newIntegration.Id;
                    //modelProps.FeatureCols = newIntegration.Fields; 
                    var newModel = await _modelService.CreateEmptyModel(user, modelProps);
                    _notifications.SendNewIntegrationSummary(integrationWithDescription, user);
                    var schema = newIntegration.Fields.Select(x => _mapper.Map<FieldDefinitionViewModel>(x));
                    var integrationViewModel = new IntegrationSchemaViewModel(newIntegration.Id, schema);
                    var modelViewModel = _mapper.Map<Model, ModelViewModel>(newModel);
                    return Json(new NewModelViewModel(modelViewModel, integrationViewModel));
                }
            }
            catch (Exception ex)
            {
                var resp = Json(new {success=false, message=ex.Message});
                resp.StatusCode = 500;
                return resp;
            }
            return null;
        }
    }
}
