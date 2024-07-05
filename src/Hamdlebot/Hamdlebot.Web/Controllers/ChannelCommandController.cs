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
    [Route("api/channel/{channelId}/commands")]
    [Authorize]
    [ApiController]
    public class ChannelCommandController : ControllerBase
    {
        private readonly HamdlebotContext _dbContext;
        private readonly IAuthenticatedTwitchUser _twitchUser;
        private readonly IBus _bus;

        public ChannelCommandController(
            HamdlebotContext dbContext,
            IAuthenticatedTwitchUser twitchUser,
            IBus bus)
        {
            _dbContext = dbContext;
            _twitchUser = twitchUser;
            _bus = bus;
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
                var endpoint = await _bus.GetSendEndpoint(new Uri($"queue:{MassTransitReceiveEndpoints.TwitchChannelSettingsUpdatedConsumer}-{botChannel.TwitchUserId}"));
                await endpoint.Send(new TwitchChannelUpdateMessage
                {
                    Action = ActionType.UpdateChannel,
                    Channel = new Channel(botChannel)
                });
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
                var endpoint = await _bus.GetSendEndpoint(new Uri($"queue:{MassTransitReceiveEndpoints.TwitchChannelSettingsUpdatedConsumer}-{botChannel.TwitchUserId}"));
                await endpoint.Send(new TwitchChannelUpdateMessage
                {
                    Action = ActionType.UpdateChannel,
                    Channel = new Channel(botChannel)
                });
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
                var endpoint = await _bus.GetSendEndpoint(new Uri($"queue:{MassTransitReceiveEndpoints.TwitchChannelSettingsUpdatedConsumer}-{botChannel.TwitchUserId}"));
                await endpoint.Send(new TwitchChannelUpdateMessage
                {
                    Action = ActionType.UpdateChannel,
                    Channel = new Channel(botChannel)
                });
            }

            return Ok();
        }
    }
}
