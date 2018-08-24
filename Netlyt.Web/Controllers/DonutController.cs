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
using Netlyt.Data.ViewModels;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Service.Data;

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
        private UserManager<User> _userManager;

        public DonutController(IMapper mapper,
            IOrionContext behaviourCtx,
            UserManager<User> userManager,
            ModelService modelService,
            IDonutService donutService)
        {
            _mapper = mapper;
            //_modelContext = typeof(Model).GetDataSource<Model>(); 
            _orionContext = behaviourCtx;
            _modelService = modelService;
            _donutService = donutService;
            _userManager = userManager;
        }

        [HttpGet("/donut/{id}", Name = "GetDonutById")]
        public async Task<IActionResult> GetById(long id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            try
            {
                var result = await _donutService.GeneratePythonModule(id, user);
                return Json(new
                {
                    code = result.Item1,
                    donutScript = result.Item2.ToString(),
                    donut = _mapper.Map<DonutScriptViewModel>(result.Item2)
                });
            }
            catch (NotFound ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
