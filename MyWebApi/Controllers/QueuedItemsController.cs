using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QueuedItemsController : ControllerBase
    {

        private readonly ILogger<QueuedItemsController> _logger;
        private readonly QueueManagementService queueManagementService;

        public QueuedItemsController(ILogger<QueuedItemsController> logger, QueueManagementService queueManagement)
        {
            _logger = logger;
            queueManagementService = queueManagement;
        }

        /// <summary>
        /// Uses LRANGE to retrieve queued items if there are any.
        /// </summary>
        /// <returns>A list of queued items.</returns>
        [HttpGet]
        public List<string> Get()
        {
            return queueManagementService.GetQueuedItems();
        }

        /// <summary>
        /// Creates a new entry in the list via RPUSH
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Post()
        {
            string strGuid = Guid.NewGuid().ToString();
            await queueManagementService.AddItem(strGuid);
            var res = new JsonResult(strGuid);
            res.StatusCode = StatusCodes.Status201Created;
            return res;
        }

        /// <summary>
        /// Pops the next item out of the queue using LPOP and returns it.
        /// </summary>
        /// <returns>The string from the left side of the list.</returns>
        [HttpGet]
        [Route("next")]
        public async Task<IActionResult> GetNext()
        {
            var popped = await queueManagementService.RemoveItem();
            var res = new JsonResult(popped);
            res.StatusCode = StatusCodes.Status200OK;
            return res;
        }

    }
}
