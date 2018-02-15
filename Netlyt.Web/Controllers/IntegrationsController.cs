using System.Collections.Generic;
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
using static Netlyt.Web.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Netlyt.Web.Helpers;
using Newtonsoft.Json.Linq;

namespace Netlyt.Web.Controllers
{
    [Route("integration")]
    public class IntegrationsController : Controller
    {
        private RemoteDataSource<DataIntegration> _integrationContext;
        private readonly UserManager<User> _userManager;
        // GET: /<controller>/
        public IntegrationsController(UserManager<User> userManager)
        {
            _integrationContext = typeof(DataIntegration).GetDataSource<DataIntegration>();
            _userManager = userManager;
        }

        [HttpGet("integration/all")]
        [Authorize]
        public IEnumerable<DataIntegration> GetAll([FromQuery] int page)
        {
            var user = _userManager.GetUserAsync(User).Result;
            int pageSize = 25;
            return _integrationContext.Where(x => x.Owner == user).Skip(page * pageSize).Take(pageSize).ToList();
        }

        [HttpGet("{id}", Name = "GetIntegration")]
        [Authorize]
        public IActionResult GetById(long id, string target, string attr, string script)
        {
            var item = _integrationContext.FirstOrDefault(t => t.Id == id);
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
        [HttpPost]
        [Authorize]
        [DisableFormValueModelBinding]
        public async Task<IActionResult> Create()
        {
            DataIntegration integration = null;
            if (Request.GetMultipartBoundary() == null)
            {
                var model = JObject.Parse(await Request.GetRawBodyString());
                //regular post do something with this?
            }
            else
            {
                // borrowed from https://dotnetcoretutorials.com/2017/03/12/uploading-files-asp-net-core/
                FormValueProvider formModel;
                var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                using (var stream = System.IO.File.Create(path))
                {
                    formModel = await Request.StreamFile(stream);
                }
                integration = new DataIntegration();

                var bindingSuccessful = await TryUpdateModelAsync(integration, prefix: "",
                   valueProvider: formModel);

                if (!bindingSuccessful)
                {
                    if (!ModelState.IsValid)
                    {
                        return BadRequest(ModelState);
                    }
                }                
            }
            _integrationContext.Add(integration);
            _integrationContext.Save(integration);
            return CreatedAtRoute("GetIntegration", new { id = integration.Id }, integration);
        }

        [HttpPut("{id}")]
        [Authorize]
        public IActionResult Update(long id, [FromBody] DataIntegrationUpdateViewModel item)
        {
            if (item == null || item.Id != id)
            {
                return BadRequest();
            }
            var integration = _integrationContext.FirstOrDefault(t => t.Id == id);
            if (integration == null)
            {
                return NotFound();
            }
            //TODO: add logic to check if we're updating integrations or what

            _integrationContext.Update(integration);
            // _integrationContext.Save();
            return new NoContentResult();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public IActionResult Delete(long id)
        {
            var item = _integrationContext.FirstOrDefault(t => t.Id == id);
            if (item == null)
            {
                return NotFound();
            }

            _integrationContext.Remove(item);
            return new NoContentResult();
        }

        [HttpGet("{id}/schema")]
        public IActionResult GetSchema(long id)
        {
            var item = _integrationContext.FindFirst(x => x.Id == id);
            if (item == null)
            {
                return new NotFoundResult();
            }
            return Ok(item.Fields);
        }
    }
}
