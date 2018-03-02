using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using nvoid.db.Extensions;
using Newtonsoft.Json.Linq;
using Netlyt.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using nvoid.db;
using Netlyt.Service.Ml;
using Netlyt.Web.ViewModels;

namespace Netlyt.Web.Controllers
{
    [Route("model")]
    [Authorize]
    public class ModelController : Controller
    {
        private BehaviourContext _orionContext;
        private readonly UserManager<User> _userManager;
        private UserService _userService;
        private IMapper _mapper;
        private ModelService _modelService;

        public ModelController(IMapper mapper,
            BehaviourContext behaviourCtx,
            UserManager<User> userManager,
            UserService userService,
            ModelService modelService)
        {
            _mapper = mapper;
            //_modelContext = typeof(Model).GetDataSource<Model>();
            _userManager = userManager;
            _orionContext = behaviourCtx;
            _userService = userService;
            _modelService = modelService;
        }

        [HttpGet("/all")]
        public async Task<IEnumerable<ModelViewModel>> GetAll([FromQuery] int page)
        {
            var userModels = await _userService.GetMyModels(page);
            var viewModels = userModels.Select(m => _mapper.Map<ModelViewModel>(m));
            return viewModels;
        }

        [HttpGet("/paramlist")]
        public JsonResult GetParamsList()
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
            var item = _modelService.GetById(id);
            if (item == null)
            {
                return NotFound();
            }
            return new ObjectResult(_mapper.Map<ModelViewModel>(item));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ModelCreationViewModel item)
        {
            if (item == null)
            {
                return BadRequest();
            }
            var user = await _userService.GetCurrentUser();
            var newModel = await _modelService.CreateModel(user, item.ModelName);
            return CreatedAtRoute("GetModel", new { id = newModel.Id }, item);
        }
        [HttpPut("{id}")]
        public IActionResult Update(long id, [FromBody] ModelUpdateViewModel item)
        {
            if (item == null || item.Id != id)
            {
                return BadRequest();
            }

            var model = _modelService.GetById(id);
            if (model == null)
            {
                return NotFound();
            }

            //TODO: add logic to check if we're updating integrations or what
            //_modelService.UpdateModel(model, item);
            // _modelContext.Save();
            return new NoContentResult();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            await _userService.DeleteModel(id);
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

        [HttpPost("{id}/[action]")]
        public IActionResult Deploy(long id)
        {
            var json = Request.GetRawBodyString();
            JObject data = JObject.Parse(json.Result);
            var m = _modelService.GetById(id);
            if (m == null) return NotFound();
            m.CurrentModel = data.GetValue("current_model").ToString();
            _modelService.SaveChanges();
            return new NoContentResult();
        }

    }
}