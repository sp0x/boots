using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using nvoid.db;
using nvoid.db.Extensions;
using Netlyt.Service;
using Netlyt.Web;
using Netlyt.Web.Models.DataModels;
using Newtonsoft.Json.Linq;

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
            return Enumerable.ToList(_modelContext);
        }

        [HttpGet("/paramlist")]
        public JsonResult GetParamsList()
        {
            object param = null;
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
            item.Save();
            return CreatedAtRoute("GetModel", new { id = item.Id }, item);
        }
        [HttpPut("{id}")]
        public IActionResult Update(long id, [FromBody] Model item)
        {
            if (item == null || item.Id != id) return BadRequest();

            var model = _modelContext.FirstOrDefault(t => t.Id == id);
            if (model == null)
            {
                return NotFound();
            }

            //TODO: add logic to check if we're updating integrations or what
            model.Save();
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
            item.Save();
            return new NoContentResult();
        }

        [HttpPost("{id}/[action]")]
        public async Task<IActionResult> Train(long id)
        {
            var json = await HttpRequestExtensions.GetRawBodyString(Request);
            JObject query = JObject.Parse(json);
            //kick off background async task to train
            // return 202 with wait at endpoint
            //async task
            // do training
            // set current model to the id of whatever came back
            var m_id = _orionContext.Query(query).Result;
            return Accepted(m_id);
        }

    }
}