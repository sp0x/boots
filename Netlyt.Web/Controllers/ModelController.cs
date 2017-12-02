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
    public class ModelController : Controller
    {
        private RemoteDataSource<Model> _modelContext;
        public ModelController()
        {
            _modelContext = typeof(Model).GetDataSource<Model>();
        }
        [HttpGet]
        public IEnumerable<Model> GetAll()
        {
            return _modelContext.ToList();
        }

        [HttpGet("{id}", Name = "GetModel")]
        public IActionResult GetById(long id)
        {
            var item = _modelContext.FirstOrDefault(t => t.Id == id);
            if (item == null)
            {
                return NotFound();
            }
            return new ObjectResult(item);
        }
        [HttpPost]
        public IActionResult Create([FromBody] Model item)
        {
            if (item == null)
            {
                return BadRequest();
            }

            _modelContext.Add(item);
            _modelContext.Save();

            return CreatedAtRoute("GetModel", new { id = item.ID }, item);
        }
        [HttpPut("{id}")]
        public IActionResult Update(long id, [FromBody] Model item)
        {
            if (item == null || item.ID != id)
            {
                return BadRequest();
            }

            var model = _modelContext.FirstOrDefault(t => t.Id == id);
            if (model == null)
            {
                return NotFound();
            }

            //TODO: add logic to check if we're updating integrations or what

            _modelContext.Update(model);
            _modelContext.Save();
            return new NoContentResult();
        }


        [HttpDelete("{id}")]
        public IActionResult Delete(long id)
        {
            var item = _modelContext.FirstOrDefault(t => t.Id == id);
            if (item == null)
            {
                return NotFound();
            }

            _modelContext.Remove(item);
            _modelContext.Save();
            return new NoContentResult();
        }

    }
}