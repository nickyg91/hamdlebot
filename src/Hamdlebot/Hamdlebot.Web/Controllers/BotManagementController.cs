using Hamdlebot.Data.Contexts.Hamdlebot;
using Hamdlebot.Data.Contexts.Hamdlebot.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Hamdlebot.Web.Controllers
{
    [Route("api/hamdlebot/management")]
    [ApiController]
    [Authorize]
    public class BotManagementController : ControllerBase
    {
        private readonly HamdlebotContext _dbContext;

        public BotManagementController(HamdlebotContext dbContext)
        {
            _dbContext = dbContext;
        }
        
        [HttpPut("join-channel")]
        public async Task JoinChannel()
        {
            var channelId = 1;
            // move to service at some point.
            var channel = new BotChannel
            {
                
            };
            return;
        }
    }
}
