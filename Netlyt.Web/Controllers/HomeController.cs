using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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

        [AllowAnonymous]
        [HttpPost("/sendmail")]
        public async Task<IActionResult> Sendmail(string from, string body, string name, string forProduct = "none")
        {
            await _emailService.SendEmailAsync(from, name, body + "\nProduct: " + forProduct);
            return Redirect(HttpContext.Request.Headers["Referer"]);
        }

        [AllowAnonymous]
        [HttpGet("/sendmail")]
        public async Task<IActionResult> SendmailGet(string from, string body, string name)
        {
            throw new NotImplementedException();
            await _emailService.SendEmailAsync(from, name, body);
            return Redirect(HttpContext.Request.Headers["Referer"]);
        }
    }
}
