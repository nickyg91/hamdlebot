using Hamdlebot.Core.Exceptions;
using Hamdlebot.Core.Models;
using Hamdlebot.Data.Contexts.Hamdlebot;
using Hamdlebot.Data.Contexts.Hamdlebot.Entities;
using Hamdlebot.Models.ViewModels;
using HamdleBot.Services.Twitch.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hamdlebot.Web.Controllers
{
    [Route("api/channel/{channelId}/commands")]
    [Authorize]
    [ApiController]
    public class ChannelCommandController : ControllerBase
    {
        private readonly HamdlebotContext _dbContext;
        private readonly IAuthenticatedTwitchUser _twitchUser;
        private readonly ITwitchChatService _twitchChatService;

        public ChannelCommandController(
            HamdlebotContext dbContext,
            IAuthenticatedTwitchUser twitchUser,
            ITwitchChatService twitchChatService)
        {
            _dbContext = dbContext;
            _twitchUser = twitchUser;
            _twitchChatService = twitchChatService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddCommand(int channelId, [FromBody] ChannelCommand commandToAdd)
        {
            var channel = await _dbContext.BotChannels.FirstOrDefaultAsync(x => x.Id == channelId);
            if (channel == null)
            {
                return NotFound();
            }

            if (channel.TwitchUserId != _twitchUser.TwitchUserId)
            {
                return Forbid();
            }

            var commandExists = (await _dbContext.BotChannelCommands.FirstOrDefaultAsync(x =>
                x.BotChannelId == channelId && x.Command == commandToAdd.Command)) != null;

            if (commandExists)
            {
                throw new DuplicateCommandException($"Command {commandToAdd.Command} already exists.");
            }

            var botChannelCommand = new BotChannelCommand
            {
                Command = commandToAdd.Command,
                Response = commandToAdd.Response,
                BotChannelId = channelId
            };
            await _dbContext.BotChannelCommands.AddAsync(botChannelCommand);
            await _dbContext.SaveChangesAsync();
            var botChannel = await _dbContext.BotChannels
                .Include(x => x.BotChannelCommands)
                .FirstOrDefaultAsync(x => x.Id == channelId);
            commandToAdd.Id = botChannelCommand.Id;
            if (botChannel != null)
            {
                _twitchChatService.UpdateChannelSettings(botChannel);
            }

            return Ok(commandToAdd);
        }

        [HttpDelete("remove/{commandId}")]
        public async Task<IActionResult> RemoveCommand(int channelId, int commandId)
        {
            var channel = await _dbContext.BotChannels.FirstOrDefaultAsync(x => x.Id == channelId);
            if (channel == null)
            {
                return NotFound();
            }

            if (channel.TwitchUserId != _twitchUser.TwitchUserId)
            {
                return Forbid();
            }

            var botChannelCommand = await _dbContext.BotChannelCommands.FirstOrDefaultAsync(x => x.Id == commandId);
            if (botChannelCommand == null)
            {
                return NotFound();
            }

            _dbContext.BotChannelCommands.Remove(botChannelCommand);
            await _dbContext.SaveChangesAsync();
            var botChannel = await _dbContext.BotChannels
                .Include(x => x.BotChannelCommands)
                .FirstOrDefaultAsync(x => x.Id == channelId);
            if (botChannel != null)
            {
                _twitchChatService.UpdateChannelSettings(botChannel);
            }

            return Ok();
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateCommand(int channelId, [FromBody] ChannelCommand command)
        {
            var channel = await _dbContext.BotChannels.FirstOrDefaultAsync(x => x.Id == channelId);
            if (channel == null)
            {
                return NotFound();
            }

            if (channel.TwitchUserId != _twitchUser.TwitchUserId)
            {
                return Forbid();
            }

            var botChannelCommand = await _dbContext.BotChannelCommands.FirstOrDefaultAsync(x => x.Id == command.Id);
            if (botChannelCommand == null)
            {
                return NotFound();
            }

            botChannelCommand.Command = command.Command;
            botChannelCommand.Response = command.Response;
            await _dbContext.SaveChangesAsync();
            var botChannel = await _dbContext.BotChannels
                .Include(x => x.BotChannelCommands)
                .FirstOrDefaultAsync(x => x.Id == channelId);
            if (botChannel != null)
            {
                _twitchChatService.UpdateChannelSettings(botChannel);
            }

            return Ok();
        }
    }
}
