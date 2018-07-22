using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.Configuration;
using Donut;
using Donut.Orion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.Web.ViewModels;

namespace Netlyt.Web.Controllers
{

    [Route("donut")]
    [Authorize]
    public class DonutController : Controller
    {
        private IMapper _mapper;
        private IOrionContext _orionContext;
        private ModelService _modelService;
        private IDonutService _donutService;

        public DonutController(IMapper mapper,
            IOrionContext behaviourCtx,
            ModelService modelService,
            IDonutService donutService)
        {
            _mapper = mapper;
            //_modelContext = typeof(Model).GetDataSource<Model>(); 
            _orionContext = behaviourCtx;
            _modelService = modelService;
            _donutService = donutService;
        }

        [HttpGet("/donut/{id}", Name = "GetDonutById")]
        public async Task<IActionResult> GetById(long id)
        {
            var item = _modelService.GetById(id);
            if (item == null) return NotFound();
            var donut = (DonutScriptInfo)item.DonutScript;
            if (donut == null)
            {
                return NotFound();
            }
            var pythonCode = await _donutService.ToPythonModule(donut);
            return Json(new
            {
                code= pythonCode,
                donutScript = donut.ToString(),
                donut= _mapper.Map<DonutScriptViewModel>(donut)
            });
        }
    }
}
