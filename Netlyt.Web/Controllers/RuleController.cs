using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using nvoid.db.DB.RDS;
using nvoid.db.Extensions;
using nvoid.Integration;
using Netlyt.Web.Models.DataModels;
using Microsoft.AspNetCore.Identity;

namespace Netlyt.Web.Controllers
{
    [Route("api/[controller]")]
    public class RuleController: Controller
    {
        private RemoteDataSource<Rule> _ruleContext;

        private readonly UserManager<User> _userManager;
        public RuleController(UserManager<User> userManager) {
             _ruleContext = typeof(Rule).GetDataSource<Rule>();
             _userManager = userManager;
        }
        [HttpGet("/rules")]
        public IEnumerable<Model> GetAll([FromQuery] int page)
        {
            var user = _userManager.GetUserAsync(User).Result;
            int pageSize = 25;
            return _ruleContext.Where(x => x.Owner==user).Skip(page * pageSize).Take(pageSize).ToList();
        }

        [HttpGet("{id}", Name = "GetRule")]
        public IActionResult GetById(long id)
        {
            var item = _ruleContext.FirstOrDefault(t => t.Id == id);
            if (item == null)
            {
                return NotFound();
            }
            return new ObjectResult(item);
        }
        [HttpPost]
        public IActionResult Create([FromBody] Rule item)
        {
            if (item == null)
            {
                return BadRequest();
            }

            _ruleContext.Add(item);
            _ruleContext.Save(item);

            return CreatedAtRoute("GetRule", new { id = item.Id }, item);
        }
        [HttpPut("{id}")]
        public IActionResult Update(long id, [FromBody] Rule item)
        {
            if (item == null || item.Id != id)
            {
                return BadRequest();
            }

            var rule = _ruleContext.FirstOrDefault(t => t.Id == id);
            if (rule == null)
            {
                return NotFound();
            }

            //TODO: add logic to check what we're updating

            _ruleContext.Update(rule);
            return new NoContentResult();
        }


        [HttpDelete("{id}")]
        public IActionResult Delete(long id)
        {
            var item = _ruleContext.FirstOrDefault(t => t.Id == id);
            if (item == null)
            {
                return NotFound();
            }

            _ruleContext.Remove(item);
            return new NoContentResult();
        }
        
    }
}