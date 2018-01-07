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


namespace Netlyt.Web.Controllers
{
    [Route("api/[controller]")]
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

        [HttpGet("/")]
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
        public IActionResult Create([FromBody] DataIntegration item)
        {
            if (item == null)
            {
                return BadRequest();
            }

            _integrationContext.Add(item);
            _integrationContext.Save(item);

            return CreatedAtRoute("GetIntegration", new { id = item.Id }, item);
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
