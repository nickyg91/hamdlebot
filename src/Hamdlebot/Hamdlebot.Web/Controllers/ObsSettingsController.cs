using System.Text.Json;
using Hamdle.Cache;
using Hamdlebot.Core;
using Hamdlebot.Core.Exceptions;
using Hamdlebot.Core.Models;
using Hamdlebot.Data.Contexts.Hamdlebot;
using Hamdlebot.Models;
using Hamdlebot.Models.Enums;
using HamdleBot.Services.OBS;
using Hamdlebot.Web.Security.Attributes;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hamdlebot.Web.Controllers
{
    [Route("api/obs-settings"), AuthorizedTwitchUser]
    [ApiController]
    public class ObsSettingsController : ControllerBase
    {
        private readonly IObsService _obsService;
        private readonly HamdlebotContext _dbContext;
        private readonly IAuthenticatedTwitchUser _authenticatedTwitchUser;
        private readonly IBus _bus;
        private readonly ICacheService _cache;

        public ObsSettingsController(
            IObsService obsService,
            HamdlebotContext dbContext,
            IAuthenticatedTwitchUser authenticatedTwitchUser,
            IBus bus,
            ICacheService cache)
        {
            _obsService = obsService;
            _dbContext = dbContext;
            _authenticatedTwitchUser = authenticatedTwitchUser;
            _bus = bus;
            _cache = cache;
        }

        [HttpPut("update")]
        public async Task SetObsSettings([FromBody] ObsSettings settings)
        {
            var channel =
                await _dbContext.BotChannels.FirstOrDefaultAsync(x =>
                    x.TwitchUserId == _authenticatedTwitchUser.TwitchUserId);
            if (channel == null)
            {
                throw new ChannelNotFoundException("Channel not found!");
            }

            channel.AllowAccessToObs = settings.IsObsEnabled;
            await _dbContext.SaveChangesAsync();
            await _cache.SetObject($"{CacheKeyType.UserObsSettings}:{_authenticatedTwitchUser.TwitchUserId}", settings);            
            var endpoint = await _bus
                .GetSendEndpoint(
                    new Uri($"queue:{MassTransitReceiveEndpoints.TwitchChannelSettingsUpdatedConsumer}-{channel.TwitchUserId}")
                    );
            
            await endpoint.Send(new TwitchChannelUpdateMessage
            {
                Action = ActionType.UpdateObsSettings,
                ObsSettings = settings
            });
        }

        [HttpGet]
        public async Task<ObsSettings?> GetObsSettings()
        {
            var obsSettings =
                await _cache.GetObject<ObsSettings>(
                    $"{CacheKeyType.UserObsSettings}:{_authenticatedTwitchUser.TwitchUserId}");
            return obsSettings;
        }

        [HttpPut("connect")]
        public async Task ConnectToObs()
        {
            var channel =
                await _dbContext.BotChannels.FirstOrDefaultAsync(x =>
                    x.TwitchUserId == _authenticatedTwitchUser.TwitchUserId);
            if (channel == null)
            {
                throw new ChannelNotFoundException("Channel not found!");
            }

            if (channel.AllowAccessToObs)
            {
                var endpoint = await _bus.GetSendEndpoint(new Uri($"queue:{MassTransitReceiveEndpoints.TwitchChannelSettingsUpdatedConsumer}-{channel.TwitchUserId}"));
                await endpoint.Send(new TwitchChannelUpdateMessage
                {
                    Action = ActionType.ConnectToObs,
                });
            }
        }
        
        [HttpPut("disconnect")]
        public async Task DisconnectFromObs()
        {
            var channel =
                await _dbContext.BotChannels.FirstOrDefaultAsync(x =>
                    x.TwitchUserId == _authenticatedTwitchUser.TwitchUserId);
            if (channel == null)
            {
                throw new ChannelNotFoundException("Channel not found!");
            }

            if (channel.AllowAccessToObs)
            {
                var endpoint = await _bus.GetSendEndpoint(new Uri($"queue:{MassTransitReceiveEndpoints.TwitchChannelSettingsUpdatedConsumer}-{channel.TwitchUserId}"));
                await endpoint.Send(new TwitchChannelUpdateMessage
                {
                    Action = ActionType.DisconnectFromObs,
                });
            }
        }
    }
}