using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Netlyt.Web.Controllers
{

    [Route("status")]
    public class StatusController:Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            return Ok();
        }
    }
}
