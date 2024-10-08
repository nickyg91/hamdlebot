using Hamdlebot.Core;
using Hamdlebot.Core.Exceptions;
using Hamdlebot.Core.Models;
using Hamdlebot.Data.Contexts.Hamdlebot;
using Hamdlebot.Data.Contexts.Hamdlebot.Entities;
using Hamdlebot.Models;
using Hamdlebot.Models.Enums;
using Hamdlebot.Models.ViewModels;
using HamdleBot.Services.Twitch.Interfaces;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hamdlebot.Web.Controllers
{
    [Route("api/hamdlebot/management")]
    [ApiController]
    [Authorize]
    public class BotManagementController : ControllerBase
    {
        private readonly HamdlebotContext _dbContext;
        private readonly IAuthenticatedTwitchUser _authenticatedTwitchUser;
        private readonly ITwitchChatService _twitchChatService;
        private readonly IBus _bus;

        public BotManagementController(
            HamdlebotContext dbContext, 
            IAuthenticatedTwitchUser authenticatedTwitchUser, 
            ITwitchChatService twitchChatService,
            IBus bus)
        {
            _dbContext = dbContext;
            _authenticatedTwitchUser = authenticatedTwitchUser;
            _twitchChatService = twitchChatService;
            _bus = bus;
        }
        
        [HttpPut("join-channel")]
        public async Task<Channel> JoinChannel()
        {
            var joinedChannel = await 
                _dbContext
                    .BotChannels
                    .Include(x => x.BotChannelCommands)
                    .FirstOrDefaultAsync(x => x.TwitchUserId == _authenticatedTwitchUser.TwitchUserId);
            Channel channel;
            if (joinedChannel != null)
            {
                channel = new Channel(joinedChannel);
                await _twitchChatService.JoinBotToChannel(channel);
                channel.Id = joinedChannel.Id;
                channel.AllowAccessToObs = joinedChannel.AllowAccessToObs;
                channel.Commands = joinedChannel.BotChannelCommands.Select(x => new ChannelCommand
                {
                    Id = x.Id,
                    Command = x.Command,
                    Response = x.Response
                }).ToList();
                channel.IsHamdleEnabled = joinedChannel.IsHamdleEnabled;
                channel.TwitchChannelName = joinedChannel.TwitchChannelName;
                channel.TwitchUserId = joinedChannel.TwitchUserId;
                return channel;
            }

            joinedChannel = new BotChannel
            {
                TwitchUserId = _authenticatedTwitchUser.TwitchUserId,
                TwitchChannelName = _authenticatedTwitchUser.TwitchUserName,
                IsHamdleEnabled = false
            };
            await _dbContext.BotChannels.AddAsync(joinedChannel);
            await _dbContext.SaveChangesAsync();
            await _twitchChatService.JoinBotToChannel(new Channel(joinedChannel));
            return new Channel(joinedChannel);
        }

        [HttpPut("leave-channel")]
        public async Task LeaveChannel()
        {
            await _twitchChatService.LeaveChannel(_authenticatedTwitchUser.TwitchUserId);
        }
        
        [HttpGet("channel")]
        public async Task<Channel?> GetChannel()
        {
            var channel = await 
                _dbContext
                    .BotChannels
                    .Include(x => x.BotChannelCommands)
                    .FirstOrDefaultAsync(x => x.TwitchUserId == _authenticatedTwitchUser.TwitchUserId);

            if (channel == null)
            {
                return null;
            }
            
            var mappedChannel = new Channel
            {
                Id = channel.Id,
                IsHamdleEnabled = channel.IsHamdleEnabled,
                Commands = channel.BotChannelCommands.Select(x => new ChannelCommand
                {
                    Id = x.Id,
                    Command = x.Command,
                    Response = x.Response,
                    BotChannelId = x.BotChannelId
                }).ToList(),
                TwitchChannelName = channel.TwitchChannelName,
                TwitchUserId = channel.TwitchUserId,
                AllowAccessToObs = channel.AllowAccessToObs
            };
            return mappedChannel;
        }
        
        [HttpPut("set-hamdle-optin/{isHamdleEnabled}")]
        public async Task<Channel> SetHamdleOptin(bool isHamdleEnabled)
        {
            var botChannel = await 
                _dbContext
                    .BotChannels
                    .Include(x => x.BotChannelCommands)
                    .FirstOrDefaultAsync(x => x.TwitchUserId == _authenticatedTwitchUser.TwitchUserId);

            if (botChannel == null)
            {
                throw new ChannelNotFoundException("Channel not found");
            }

            botChannel.IsHamdleEnabled = isHamdleEnabled;
            await _dbContext.SaveChangesAsync();
            
            var endpoint = await _bus.GetSendEndpoint(new Uri($"queue:{MassTransitReceiveEndpoints.TwitchChannelSettingsUpdatedConsumer}-{botChannel.TwitchUserId}"));
            await endpoint.Send(new TwitchChannelUpdateMessage
            {
                Action = ActionType.UpdateChannel,
                Channel = new Channel(botChannel)
            });
            
            return new Channel(botChannel);
        }
        
    }
}
