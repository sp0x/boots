using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Web.Models.ManageViewModels;

namespace Netlyt.Web.Controllers
{
    [Produces("application/json")]
    [Authorize()]
    public class DashboardController : Controller
    {
        private UserManager<User> _userManager;
        private UserService _userService;
        private RateService _rateService;

        [TempData]
        public string StatusMessage { get; set; }

        public DashboardController(
            UserService userService,
            UserManager<User> userManager,
            RateService rateService)
        {
            _userService = userService;
            _userManager = userManager;
            _rateService = rateService;
        }

        [HttpGet("/dashboard")]
        public async Task<JsonResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }
            var roles = await _userService.GetRoles(user);
            var model = new IndexViewModel
            {
                Username = user.UserName,
                Email = user.Email,
                IsEmailConfirmed = user.EmailConfirmed,
                StatusMessage = StatusMessage,
                Roles = roles,
                Id = user.Id
            };
            return Json(model);
        }

        [Authorize]
        [HttpGet("/dashboard/usage")]
        public async Task<JsonResult> GetUsage(){
            var user = await _userService.GetCurrentUser();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }
            var allowed = _rateService.GetAllowed(user);
            var usage = _rateService.GetCurrentUsageForUser(user);
            var used = allowed - usage;
            var quota = new {Used=used, Total=allowed};
            return Json(new {usage, quota});
        }
    }
}
