using System.Text.Json;
using Hamdle.Cache;
using Hamdlebot.Core;
using HamdleBot.Services.OBS;
using Hamdlebot.Web.Security.Attributes;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace Hamdlebot.Web.Controllers
{
    [Route("api/obs-settings"), AuthorizedTwitchUser]
    [ApiController]
    public class ObsSettingsController : ControllerBase
    {
        private readonly ICacheService _cacheService;
        private readonly IObsService _obsService;
        private readonly RedisChannel _obsSettingsChannel = new (RedisChannelType.ObsSettingsChanged, RedisChannel.PatternMode.Auto);

        public ObsSettingsController(ICacheService cacheService, IObsService obsService)
        {
            _cacheService = cacheService;
            _obsService = obsService;
        }
        
        [HttpPut("update")]
        public async Task SetObsSettings([FromBody] ObsSettings settings)
        {
            await _cacheService.Subscriber.PublishAsync(_obsSettingsChannel, JsonSerializer.Serialize(settings));
        }

        [HttpGet]
        public ObsSettings? GetObsSettings()
        {
            return _obsService.GetCurrentSettings();
        }
    }
}
