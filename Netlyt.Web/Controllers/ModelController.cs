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
using Netlyt.Service.Integration;
using Netlyt.Service.Ml;
using Netlyt.Service.Orion;
using Netlyt.Web.ViewModels;

namespace Netlyt.Web.Controllers
{
    [Route("model")]
    [Authorize]
    public class ModelController : Controller
    {
        private OrionContext _orionContext; 
        private UserService _userService;
        private IMapper _mapper;
        private ModelService _modelService;

        public ModelController(IMapper mapper,
            OrionContext behaviourCtx,
            UserManager<User> userManager,
            UserService userService,
            ModelService modelService)
        {
            _mapper = mapper;
            //_modelContext = typeof(Model).GetDataSource<Model>(); 
            _orionContext = behaviourCtx;
            _userService = userService;
            _modelService = modelService;
        }

        [HttpGet("/model/mymodels")]
        public async Task<IEnumerable<ModelViewModel>> GetAll([FromQuery] int page)
        {
            var userModels = await _userService.GetMyModels(page);
            var viewModels = userModels.Select(m => _mapper.Map<ModelViewModel>(m));
            return viewModels;
        }

        [HttpGet("/model/paramlist")]
        public async Task<JsonResult> GetParamsList()
        {
            var orionQuery = new OrionQuery(OrionOp.ParamList);
            var param = await _orionContext.Query(orionQuery);
            return Json(param);
        }

        [HttpGet("/model/classlist")]
        public async Task<JsonResult> GetClassList()
        {
            var query = new OrionQuery(OrionOp.GetModelList);
            var param = await _orionContext.Query(query);
            return Json(param);
        }

        [HttpGet("/model/{id}", Name = "GetById")]
        public IActionResult GetById(long id)
        {
            var item = _modelService.GetById(id);
            if (item == null)
            {
                return NotFound();
            }
            return new ObjectResult(_mapper.Map<ModelViewModel>(item));
        } 

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        [HttpPost("/model")]
        public async Task<IActionResult> Create([FromBody] ModelCreationViewModel item)
        {
            if (item == null)
            {
                return BadRequest();
            }
            var user = await _userService.GetCurrentUser();
            var integration = _userService.GetUserIntegration(user, item.DataSource);
            var relations = item.Relations?.Select(x => new FeatureGenerationRelation(x[0], x[1]));
            //This really needs a builder..
            var newModel = await _modelService.CreateModel(user,
                item.Name,
                new List<DataIntegration>(new []{integration}),
                item.Callback,
                item.GenerateFeatures,
                relations,
                item.TargetAttribute);
            return CreatedAtRoute("GetById", new { id = newModel.Id }, item);
        }

        [HttpPut("/model/{id}")]
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

        [HttpDelete("/model/{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            await _userService.DeleteModel(id);
            return new NoContentResult();
        }

        [HttpPost("/model/{id}/[action]")]
        public async Task<IActionResult> Train(long id)
        {
            var json = await Request.GetRawBodyString();
            JObject query = JObject.Parse(json);
            //kick off background async task to train
            // return 202 with wait at endpoint
            //async task
            // do training
            // set current model to the id of whatever came back
            var m_id = await _orionContext.Query(query);
            return Accepted(m_id);
        }

        [HttpPost("/model/{id}/[action]")]
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