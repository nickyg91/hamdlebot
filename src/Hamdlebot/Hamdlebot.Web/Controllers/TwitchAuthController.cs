using Hamdle.Cache;
using Hamdlebot.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Hamdlebot.Web.Controllers
{
    [Route("api/twitch/auth")]
    [ApiController]
    public class TwitchAuthController : ControllerBase
    {
        private readonly IOptions<AppConfigSettings> _appConfigSettings;

        public TwitchAuthController(
            IOptions<AppConfigSettings> appConfigSettings)
        {
            _appConfigSettings = appConfigSettings;
        }
        
        [HttpGet]
        public string Get()
        {
            return $"https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={_appConfigSettings.Value.TwitchConnectionInfo.ClientId}&redirect_uri=http://localhost:3000&scope=chat%3Aread+chat%3Aedit";
        }

        // [HttpPut("{code}")]
        // public async Task<IActionResult> Put(string code)
        // {
        //     await _cacheService.Subscriber.PublishAsync(new RedisChannel("twitch_auth_code", RedisChannel.PatternMode.Auto), code);
        //     return Ok();
        // }
    }
}
