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
        private UserService _userService;
        private IMapper _mapper;

        private IOrionContext _orionContext;
        private INotificationService _notifications;
        private ICloudNodeService _nodeService;

        // GET: /<controller>/
        public IntegrationsController(UserService userService,
            IIntegrationService integrationService,
            IMapper mapper,
            IOrionContext orionContext,
            INotificationService notificationsService,
            ICloudNodeService nodeService)
        {
            _userService = userService;
            _integrationService = integrationService;
            _mapper = mapper;
            _orionContext = orionContext;
            _notifications = notificationsService;
            _nodeService = nodeService;
        }

        [HttpGet("/integrations/me")]
        [Authorize]
        public async Task<IEnumerable<DataIntegrationViewModel>> GetAll([FromQuery] int page)
        {
            var user = await _userService.GetCurrentUser();
            int pageSize = 25;
            var dataIntegrations = await _userService.GetIntegrations(user, page, pageSize);
            return dataIntegrations.Select(x => _mapper.Map<DataIntegrationViewModel>(x));
        }

        [HttpGet("/integration/{id}", Name = "GetIntegration")]
        [Authorize]
        public IActionResult GetById(long id, string target, string attr, string script)
        {
            var item = _integrationService.GetById(id, withPermissions:true);
            if (item == null)
            {
                return NotFound();
            }
            if (target != null)
            {
                if (script == null || attr == null)
                {
                    return BadRequest();
                }
                //kick the async
                return Accepted();
            }
            return new ObjectResult(item);
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
            var newIntegration = await _integrationService.Create(integration.Name, integration.DataFormatType);
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
                    result = await _integrationService.CreateOrAppendToIntegration(targetStream, fileContentType,
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

        [HttpGet("{id}/schema")]
        public async Task<IActionResult> GetSchema(long id)
        {
            try
            {
                var schema = await _integrationService.GetSchema(id);
                _notifications.SendIntegrationViewed(id);
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
                var result = await _integrationService.CreateOrAppendToIntegration(Request);
                if (result != null)
                {
                    IIntegration newIntegration = result.Integration;
                    _notifications.SendNewIntegrationSummary(newIntegration);
                    var schema = newIntegration.Fields.Select(x => _mapper.Map<FieldDefinitionViewModel>(x));
                    return Json(new IntegrationSchemaViewModel(newIntegration.Id, schema));
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
