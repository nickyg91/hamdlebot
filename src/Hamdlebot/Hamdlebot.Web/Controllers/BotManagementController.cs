using Hamdlebot.Core.Models;
using Hamdlebot.Data.Contexts.Hamdlebot;
using Hamdlebot.Data.Contexts.Hamdlebot.Entities;
using HamdleBot.Services.Twitch.Interfaces;
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
        public BotManagementController(
            HamdlebotContext dbContext, 
            IAuthenticatedTwitchUser authenticatedTwitchUser, 
            ITwitchChatService twitchChatService)
        {
            _dbContext = dbContext;
            _authenticatedTwitchUser = authenticatedTwitchUser;
            _twitchChatService = twitchChatService;
        }
        
        [HttpPut("join-channel")]
        public async Task<BotChannel> JoinChannel()
        {
            var joinedChannel = await 
                _dbContext.BotChannels.FirstOrDefaultAsync(x => x.TwitchUserId == _authenticatedTwitchUser.TwitchUserId);
            if (joinedChannel != null)
            {
                await _twitchChatService.JoinBotToChannel(joinedChannel);
                return joinedChannel;
            }

            var channel = new BotChannel
            {
                TwitchUserId = _authenticatedTwitchUser.TwitchUserId,
                TwitchChannelName = _authenticatedTwitchUser.TwitchUserName,
                IsHamdleEnabled = false
            };
            await _dbContext.BotChannels.AddAsync(channel);
            await _dbContext.SaveChangesAsync();
            await _twitchChatService.JoinBotToChannel(channel);
            return channel;
        }

        [HttpPut("leave-channel")]
        public async Task LeaveChannel()
        {
            await _twitchChatService.LeaveChannel(_authenticatedTwitchUser.TwitchUserId);
        }
        
        public async Task<BotChannel?> GetChannel()
        {
            return await 
                _dbContext
                    .BotChannels
                    .FirstOrDefaultAsync(x => x.TwitchUserId == _authenticatedTwitchUser.TwitchUserId);
        }
    }
}
