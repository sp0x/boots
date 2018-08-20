using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Donut;
using Donut.Integration;
using Microsoft.AspNetCore.Mvc;
using Netlyt.Data.ViewModels;
using Netlyt.Service.Helpers;

namespace Netlyt.Client.Controllers
{
    [Route("integration")]
    public class IntegrationsController : Controller
    {
        private IIntegrationService _integrationService;
        private IMapper _mapper;

        public IntegrationsController(IIntegrationService integrationService,
            IMapper mapper)
        {
            _integrationService = integrationService;
            _mapper = mapper;
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
                    var schema = newIntegration.Fields.Select(x => _mapper.Map<FieldDefinitionViewModel>(x));
                    return Json(new IntegrationSchemaViewModel(newIntegration.Id, schema));
                }
            }
            catch (Exception ex)
            {
                var resp = Json(new { success = false, message = ex.Message });
                resp.StatusCode = 500;
                return resp;
            }

            return null;
        }
    }
}
