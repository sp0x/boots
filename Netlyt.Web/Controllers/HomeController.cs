using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Netlyt.Service;
using Netlyt.Web.Models;

namespace Netlyt.Web.Controllers
{
    public class HomeController : Controller
    {
        private IEmailSender _emailService;

        public HomeController(IEmailSender emailer)
        {
            _emailService = emailer;
        }
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost("/sendmail")]
        public async Task<IActionResult> Sendmail(string from, string body, string name)
        {
            await _emailService.SendEmailAsync(from, name, body);
            return Redirect(HttpContext.Request.Headers["Referer"]);
        }
    }
}
