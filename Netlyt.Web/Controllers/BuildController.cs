using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Donut;
using Donut.Orion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using Netlyt.Interfaces.Data;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Web.Models.ManageViewModels;
using Newtonsoft.Json.Linq;

namespace Netlyt.Web.Controllers
{

    [Produces("application/json")]
    [Authorize()]
    public class BuildController : Controller
    {
        private UserManager<User> _userManager;
        private ModelService _modelService;
        private IOrionContext _orion;
        private IDonutService _donut;
        private IIntegrationService _integrationService;

        public BuildController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IEmailSender emailSender,
            ILogger<DashboardController> logger,
            ModelService modelService,
            UrlEncoder urlEncoder,
            IOrionContext orionContext,
            IDonutService donut,
            IIntegrationService integrations)
        {
            _modelService = modelService;
            _userManager = userManager;
            _orion = orionContext;
            _donut = donut;
            _integrationService = integrations;
        }

        [HttpGet("/build/{buildId}/getSnippets")]
        public async Task<IActionResult>  GetSnippet(long buildId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            var snippets = await _modelService.GetSnippets(user, buildId);
            return Json(snippets);
        }

        [HttpGet("/build/{buildId}/exportProject")]
        public async Task<IActionResult> ExportPythonProject(long buildId)
        {
            try
            {
                JToken projectRes = await _modelService.ExportPy(buildId);
                string projectZip = projectRes["project"].ToString();
                var assetFile = _orion.GetExperimentAsset(projectZip);
                if (assetFile == null)
                {
                    return NotFound();
                }
                else
                {
                    var assetName = Path.GetFileName(assetFile);
                    var bytes = System.IO.File.Open(assetFile, FileMode.Open);
                    return File(bytes, "application/force-download", assetName);
                }
            }
            catch (NotFound nf)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        [HttpGet("/build/{buildId}/getSample")]
        public async Task<IActionResult> GetTestSample(long buildId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            var trainingTask = _modelService.GetBuildById(buildId, user);
            if (trainingTask == null) return NotFound();
            if (trainingTask.Performance is null) return NotFound();
            var file = trainingTask.Performance.TestResultsUrl;
            var assetFile = _orion.GetExperimentAsset(file);
            if (assetFile == null)
            {
                return NotFound();
            }
            else
            {
                var assetName = Path.GetFileName(assetFile);
                var bytes = System.IO.File.Open(assetFile, FileMode.Open);
                return File(bytes, "application/force-download", assetName);
            }
        }
    }
}
