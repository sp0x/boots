using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using nvoid.db.DB.RDS;
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
        public IEnumerable<Model> GetAll()
        {
            return (IEnumerable<Model>)_ruleContext.ToList();
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

            _ruleContext.Add(item);
           _ruleContext.Save(item);

            return CreatedAtRoute("GetRule", new { id = item.ID }, item);
        }
        [HttpPut("{id}")]
        public IActionResult Update(long id, [FromBody] Rule item)
        {
            if (item == null || item.ID != id)
            {
                return BadRequest();
            }

            var rule = _ruleContext.FirstOrDefault(t => t.ID == id);
            if (rule == null)
            {
                return NotFound();
            }

            rule.IsActive = item.IsActive;
            rule.Models = item.Models;
            
            _ruleContext.Update(rule);
            //_ruleContext.Save();
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

            _ruleContext.Remove(item);
            //_ruleContext.Save();
            return new NoContentResult();
        }
        
    }
}