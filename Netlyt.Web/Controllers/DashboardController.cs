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
using Netlyt.Web.ViewModels;

namespace Netlyt.Web.Controllers
{
    [Produces("application/json")]
    [Authorize()]
    public class DashboardController : Controller
    {
        private UserManager<User> _userManager;
        private SignInManager<User> _signInManager;
        private IEmailSender _emailSender;
        private ILogger<DashboardController> _logger;
        private UrlEncoder _urlEncoder;

        [TempData]
        public string StatusMessage { get; set; }

        public DashboardController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IEmailSender emailSender,
            ILogger<DashboardController> logger,
            UrlEncoder urlEncoder)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _urlEncoder = urlEncoder;
        }

        [HttpGet("/dashboard")]
        public async Task<JsonResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }
            var model = new IndexViewModel
            {
                Username = user.UserName,
                Email = user.Email,
                IsEmailConfirmed = user.EmailConfirmed,
                StatusMessage = StatusMessage
            };
            return Json(model);
        }
    }
}
