using IotCoreWebSocketProxy.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IotCoreWebSocketProxy.Controllers
{
    [ApiController]
    public class MessageController : ControllerBase
    {

        [Route("api/message/send")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult SendMessage(SendMessageModel model) =>
         Ok(model);
    }
}
