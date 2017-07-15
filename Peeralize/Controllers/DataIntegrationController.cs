using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using nvoid.Integration;

namespace Peeralize.Controllers
{
    [Produces("application/json")]
    [Route("api/data")]
    public class DataIntegrationController : Controller
    {
        [HttpPost]
        public ActionResult AddEntity(string entryBlob)
        {

            


        }

    }
}