using System.Text.Json;
using Hamdle.Cache;
using Hamdlebot.Core;
using Hamdlebot.Core.Exceptions;
using Hamdlebot.Core.Models;
using Hamdlebot.Data.Contexts.Hamdlebot;
using HamdleBot.Services.OBS;
using Hamdlebot.Web.Security.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Hamdlebot.Web.Controllers
{
    [Route("api/obs-settings"), AuthorizedTwitchUser]
    [ApiController]
    public class ObsSettingsController : ControllerBase
    {
        private readonly ICacheService _cacheService;
        private readonly IObsService _obsService;
        private readonly HamdlebotContext _dbContext;
        private readonly IAuthenticatedTwitchUser _authenticatedTwitchUser;

        private readonly RedisChannel _obsSettingsChannel =
            new(RedisChannelType.ObsSettingsChanged, RedisChannel.PatternMode.Auto);

        public ObsSettingsController(
            ICacheService cacheService,
            IObsService obsService,
            HamdlebotContext dbContext,
            IAuthenticatedTwitchUser authenticatedTwitchUser)
        {
            _cacheService = cacheService;
            _obsService = obsService;
            _dbContext = dbContext;
            _authenticatedTwitchUser = authenticatedTwitchUser;
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

        [HttpPut("update-enabled-status/{isEnabled}")]
        public async Task ToggleObsEnabled(bool isEnabled)
        {
            var channel = await _dbContext.BotChannels
                .FirstOrDefaultAsync(x => x.TwitchUserId == _authenticatedTwitchUser.TwitchUserId);

            if (channel == null)
            {
                throw new ChannelNotFoundException("Channel not found");
            }
            
            channel.AllowAccessToObs = isEnabled;
            await _dbContext.SaveChangesAsync();

            if (!isEnabled)
            {
                // stop OBS connection for this specific channel.
            }
        }
    }
}