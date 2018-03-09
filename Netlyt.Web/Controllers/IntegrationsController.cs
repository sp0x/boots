using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using nvoid.db; 
using nvoid.db.Extensions;
using Netlyt.Service;
using Netlyt.Service.Integration;
using Netlyt.Web.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using System.IO;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using static Netlyt.Web.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Netlyt.Service.Data;
using Netlyt.Service.Integration.Import;
using Netlyt.Web.Helpers;
using Netlyt.Web.Services;
using Netlyt.Web.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netlyt.Web.Controllers
{
    [Route("integration")]
    public class IntegrationsController : Controller
    { 
        private ApiService _apiService;
        private RemoteDataSource<IntegratedDocument> _documentStore;
        private IntegrationService _integrationService;
        private FormOptions _defaultFormOptions;
        private UserService _userService;
        private IMapper _mapper;
        // GET: /<controller>/
        public IntegrationsController(UserManager<User> userManager,
            IUserStore<User> userStore,
            OrionContext behaviourCtx,
            ManagementDbContext context,
            SocialNetworkApiManager socNetManager,
            ApiService apiService,
            UserService userService,
            IntegrationService integrationService,
            IActionDescriptorCollectionProvider actionDescriptorCollectionProvider,
            IMapper mapper)
        {
            _apiService = apiService; 
            _documentStore = typeof(IntegratedDocument).GetDataSource<IntegratedDocument>(); 
            _defaultFormOptions = new FormOptions();
            _userService = userService;
            _integrationService = integrationService;
            _mapper = mapper;
        }

        [HttpGet("/integrations/me")]
        [Authorize]
        public async Task<IEnumerable<DataIntegration>> GetAll([FromQuery] int page)
        {
            var user = await _userService.GetCurrentUser();
            int pageSize = 25;
            return await _userService.GetIntegrations(user, page, pageSize);
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
            if (target!=null) {
                if (script==null || attr==null) {
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
            return CreatedAtRoute("GetIntegration", new { id = newIntegration.Id }, _mapper.Map< DataIntegrationViewModel>(newIntegration));

        }

        [HttpPost("/integration/file")] 
        [DisableFormValueModelBinding]
        [Authorize] 
        public async Task<IActionResult> CreateFileIntegration()
        {
            DataIntegration newIntegration = null; 
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
            }
            var formAccumulator = new Dictionary<string, string>();
            var mediaTypeHeaderValue = MediaTypeHeaderValue.Parse(Request.ContentType);
            var boundary = MultipartRequestHelper.GetBoundary(
                mediaTypeHeaderValue, 1242134123);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);
            var section = await reader.ReadNextSectionAsync();
            NewIntegrationViewModel integrationParams = new NewIntegrationViewModel();
            string targetFilePath = Path.GetTempFileName();
            string fileContentType = null;
            while (section != null)
            {
                ContentDispositionHeaderValue contentDisposition;
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out contentDisposition);
                
                if (hasContentDispositionHeader)
                {
                    if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                    {
                        fileContentType = section.ContentType;
                        if (fileContentType == "application/octet-stream")
                        {
                            fileContentType = MimeResolver.Resolve(contentDisposition);
                        }
                        if (!_integrationService.MimeIsAllowed(fileContentType))
                        {
                            return Forbid("Given mime is forbidden!");
                        }
                        using (var targetStream = System.IO.File.Create(targetFilePath))
                        {
                            await section.Body.CopyToAsync(targetStream);

                            Debug.WriteLine($"Copied the uploaded file '{targetFilePath}'");
                        }
                    }
                    else if (MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition))
                    {
                        // Content-Disposition: form-data; name="key"
                        //
                        // value

                        // Do not limit the key name length here because the 
                        // multipart headers length limit is already in effect.
                        var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name);
                        var encoding = GetEncoding(section);
                        using (var streamReader = new StreamReader(
                            section.Body,
                            encoding,
                            detectEncodingFromByteOrderMarks: true,
                            bufferSize: 1024,
                            leaveOpen: true))
                        {
                            // The value length limit is enforced by MultipartBodyLengthLimit
                            var value = await streamReader.ReadToEndAsync();
                            if (String.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase))
                            {
                                value = String.Empty;
                            }
                            formAccumulator[key.ToString()] = value;

                            if (formAccumulator.Keys.Count > _defaultFormOptions.ValueCountLimit)
                            {
                                throw new InvalidDataException($"Form key count limit {_defaultFormOptions.ValueCountLimit} exceeded.");
                            }
                        }
                    }
                }
                // Drains any remaining section body that has not been consumed and
                // reads the headers for the next section.
                section = await reader.ReadNextSectionAsync();
            }
            if (!formAccumulator.ContainsKey("integration")) return BadRequest("No integration given.");
            integrationParams = JsonConvert.DeserializeObject<NewIntegrationViewModel>(formAccumulator["integration"]);
            DataImportResult result=null;
            using (var targetStream = System.IO.File.OpenRead(targetFilePath))
            {
                try
                {
                    result = await _integrationService.CreateOrFillIntegration(targetStream, fileContentType,
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
            var item = _integrationService.GetById(id);
            if (item == null)
            {
                return NotFound();
            }
            _integrationService.Remove(item);
            return new NoContentResult();
        }

        [HttpGet("{id}/schema")]
        public IActionResult GetSchema(long id)
        {
            var item = _integrationService.GetById(id);
            if (item == null)
            {
                return new NotFoundResult();
            }
            var fields = item.Fields.Select(x => _mapper.Map<FieldDefinitionViewModel>(x));
            return Ok(fields);
        }
    }

    
}
