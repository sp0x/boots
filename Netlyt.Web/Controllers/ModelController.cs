using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using nvoid.db.DB.RDS;
using nvoid.db.Extensions;
using nvoid.Integration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Netlyt.Web.Models.DataModels;
using Netlyt.Web.Services;
using Netlyt.Services;

namespace Netlyt.Web.Controllers
{
    [Route("api/[controller]")]
    public class ModelController : Controller
    {
        private RemoteDataSource<Model> _modelContext;
        private BehaviourContext _orionContext;
        public ModelController(BehaviourContext behaviourCtx)
        {
            _modelContext = typeof(Model).GetDataSource<Model>();
            _orionContext = behaviourCtx;
        }
        [HttpGet]
        public IEnumerable<Model> GetAll()
        {
            return _modelContext.ToList();
        }
        
        [HttpGet("/paramlist")]
        public JsonResult  GetParamsList()
        {
            var param = null;
            return Json(param);
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

        [HttpPost("{id}/[action]")]
        public IActionResult Train(long id)
        {
            var json = Request.GetRawBodyString();
            JObject o = JObject.Parse(json);
            //kick off background async task to train
            // return 202 with wait at endpoint
            //async task
            // do training
            // set current model to the id of whatever came back
            var m_id = 1;
            return Accepted(Json(new {
                model_id = m_id
            }));
        }

    }
}