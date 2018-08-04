using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using nvoid.db;
using nvoid.db.Extensions;
using Netlyt.Service;
using Netlyt.Web.Models;
using System.Threading.Tasks; 
using System.IO;
using System.Text;
using AutoMapper;
using Donut;
using Donut.Data;
using Donut.Integration;
using Donut.Orion;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using static Netlyt.Web.Attributes;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Data;
using Netlyt.Web.Helpers;
using Netlyt.Web.Services;
using Netlyt.Web.ViewModels;
using Newtonsoft.Json;

namespace Netlyt.Web.Controllers
{
    [Route("integration")]
    public class IntegrationsController : Controller
    {
        private ApiService _apiService;
        private RemoteDataSource<IntegratedDocument> _documentStore;
        private IIntegrationService _integrationService;
        private FormOptions _defaultFormOptions;
        private UserService _userService;
        private IMapper _mapper;

        private IOrionContext _orionContext;

        // GET: /<controller>/
        public IntegrationsController(UserManager<User> userManager,
            IUserStore<User> userStore,
            IOrionContext behaviourCtx,
            ManagementDbContext context,
            SocialNetworkApiManager socNetManager,
            ApiService apiService,
            UserService userService,
            IIntegrationService integrationService,
            IActionDescriptorCollectionProvider actionDescriptorCollectionProvider,
            IMapper mapper,
            IOrionContext orionContext)
        {
            _apiService = apiService;
            _documentStore = typeof(IntegratedDocument).GetDataSource<IntegratedDocument>();
            _defaultFormOptions = new FormOptions();
            _userService = userService;
            _integrationService = integrationService;
            _mapper = mapper;
            _orionContext = orionContext;
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
            var item = _integrationService.GetById(id);
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

        private Encoding GetEncoding(MultipartSection section)
        {
            return new UTF8Encoding(); //Todo: implement this
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
            var item = _integrationService.GetById(id).FirstOrDefault();
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
            var ign = _integrationService.GetById(id)
                .Include(x=>x.Fields)
                .Include(x=>x.Models)
                .ThenInclude(x=>x.Model)
                .ThenInclude(x=>x.Targets)
                .ThenInclude(x=>x.Column)
                .FirstOrDefault();
            if (ign == null)
            {
                return new NotFoundResult();
            }
            var fields = ign.Fields.Select(x => _mapper.Map<FieldDefinitionViewModel>(x));
            var schema = new IntegrationSchemaViewModel(ign.Id, fields);
            schema.Targets = ign.Models.SelectMany(x => x.Model.Targets)
                .Select(x => _mapper.Map<ModelTargetViewModel>(x));
            var targets = schema.Targets
                .Select(x =>new ModelTarget(ign.GetField(x.Id)))
                .Where(x=>x.Column!=null);
            var descQuery = OrionQuery.Factory.CreateDataDescriptionQuery(ign, targets);
            var description = await _orionContext.Query(descQuery);
            _integrationService.SetTargetTypes(ign, description);
            schema.AddDataDescription(description);
           
            return Json(schema);
        }

        [HttpPost("/integration/schema")]
        public async Task<IActionResult> UploadAndGetSchema()
        {
            IIntegration newIntegration = null;
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
            }

            try
            {
                NewIntegrationViewModel integrationParams = new NewIntegrationViewModel();
                string targetFilePath = Path.GetTempFileName();
                string fileContentType = null;
                DataImportResult result = null;
                using (var targetStream = System.IO.File.Create(targetFilePath))
                {
                    var form = await Request.StreamFile(targetStream);
                    fileContentType = form.GetValue("mime-type").ToString();
                    var filename = form.GetValue("filename")
                        .ToString().Trim('\"').Replace('.', '_').Replace('-', '_');
                    targetStream.Position = 0;
                    try
                    {
                        result = await _integrationService.CreateOrAppendToIntegration(targetStream, fileContentType,
                            filename);
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
