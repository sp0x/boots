using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Netlyt.Service;
using Netlyt.Service.Exceptions;

namespace Netlyt.Web.Controllers
{

    [Route("subscription")]
    public class SubscriptionsController : Controller
    {
        private ISubscriptionService _subscriptionService;
        private IEmailSender _emailSender;

        public SubscriptionsController(ISubscriptionService subsService, IEmailSender emailSender)
        {
            _subscriptionService = subsService;
            _emailSender = emailSender;
        }

        [HttpPost]
        public async Task<ActionResult> GetAccess(string email)
        {
            try
            {
                var newToken = _subscriptionService.SubscribeForAccess(email, "Netlyt", true);
                return Json(new { success = true });
            }
            catch (SubscriptionAlreadyExists ex)
            {
                return Json(new { success = false, message=ex.Message });
            }
        }
    }
}
