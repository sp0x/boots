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
using Netlyt.Service;
using Netlyt.Web;
using Microsoft.AspNetCore.Identity;

namespace Netlyt.Web.Controllers
{
    [Route("api/[controller]")]
    public class ModelController : Controller
    {
        private RemoteDataSource<Model> _modelContext;
        private BehaviourContext _orionContext;
        private readonly UserManager<User> _userManager;
        public ModelController(BehaviourContext behaviourCtx,
                               UserManager<User> userManager)
        {
            _modelContext = typeof(Model).GetDataSource<Model>();
            _userManager = userManager;
            _orionContext = behaviourCtx;
        }
        [HttpGet("/models")]
        public IEnumerable<Model> GetAll([FromQuery] int page)
        {
            var user = _userManager.GetUserAsync(User).Result;
            int pageSize = 25;
            return _modelContext.Where(x => x.User==user).Skip(page * pageSize).Take(pageSize).ToList();
        }
        
        [HttpGet("/paramlist")]
        public JsonResult  GetParamsList()
        {
            JObject query = new JObject();
            query.Add("op", 104);
            var param = _orionContext.Query(query).Result;
            return Json(param);
        }

        [HttpGet("/classlist")]
        public JsonResult GetClassList()
        {
            JObject query = new JObject();
            query.Add("op", 105);
            var param = _orionContext.Query(query).Result;
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
            _modelContext.Save(item);

            return CreatedAtRoute("GetModel", new { id = item.Id }, item);
        }
        [HttpPut("{id}")]
        public IActionResult Update(long id, [FromBody] Model item)
        {
            if (item == null || item.Id != id)
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
           // _modelContext.Save();
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
            return new NoContentResult();
        }

        [HttpPost("{id}/[action]")]
        public IActionResult Train(long id)
        {
            var json = Request.GetRawBodyString();
            JObject query = JObject.Parse(json.Result);
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