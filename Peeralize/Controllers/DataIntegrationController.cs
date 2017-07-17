using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using nvoid.Integration;

namespace Peeralize.Controllers
{ 
    [Produces("application/json")]
    [Authorize]
    [Route("data")]
    public class DataIntegrationController : Controller
    {
        [HttpPost]
        public ActionResult AddEntity(string entryBlob)
        {
            return null;
        }

        [Route("GetStatus")]
        [HttpGet]
        public ActionResult GetStatus()
        {
            return Json(new
            {
                success = true
            });
        }

        [Route("[action]")]
        [HttpPost]
        public ActionResult ClientData()
        {
            var requestBody = (new StreamReader(Request.Body)).ReadToEnd();
            return Json(new
            {
                success = true, 
                id = 1
            });
        }

    }
}