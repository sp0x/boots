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

        [TempData]
        public string StatusMessage { get; set; }

        public DashboardController(
            UserService userService,
            UserManager<User> userManager)
        {
            _userService = userService;
            _userManager = userManager;
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
                Roles = roles
            };
            return Json(model);
        }
    }
}
