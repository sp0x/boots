using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using nvoid.db;
using nvoid.db.Extensions;
using nvoid.Integration;
using Netlyt.Web.Models.DataModels;

namespace Netlyt.Web.Controllers
{
    [Route("api/[controller]")]
    public class RuleController: Controller
    {
        private RemoteDataSource<Rule> _ruleContext;
        public RuleController() {
             _ruleContext = typeof(Rule).GetDataSource<Rule>();
            
        }
        [HttpGet]
        public IEnumerable<Rule> GetAll()
        {
            return _ruleContext.Where(r => true);
        }

        [HttpGet("{id}", Name = "GetRule")]
        public IActionResult GetById(long id)
        {
            var item = _ruleContext.FirstOrDefault(t => t.ID == id);
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
            item.Save();
//            _ruleContext.Add(item);
//            _ruleContext.Save();
            return CreatedAtRoute("GetRule", new { id = item.Id }, item);
        }
        [HttpPut("{id}")]
        public IActionResult Update(long id, [FromBody] Rule item)
        {
            if (item == null || item.Id != id)
            {
                return BadRequest();
            }

            var rule = _ruleContext.FirstOrDefault(t => t.ID == id);
            if (rule == null)
            {
                return NotFound();
            }

            rule.IsActive = item.IsActive;
            rule.Save();
//            _ruleContext.Update(rule);
//            _ruleContext.Save();
            return new NoContentResult();
        }


        [HttpDelete("{id}")]
        public IActionResult Delete(long id)
        {
            var item = _ruleContext.FirstOrDefault(t => t.ID == id);
            if (item == null)
            {
                return NotFound();
            }
            item.Save();
//            _ruleContext.Remove(item);
            //_ruleContext.Save();
            return new NoContentResult();
        }
        
    }
}