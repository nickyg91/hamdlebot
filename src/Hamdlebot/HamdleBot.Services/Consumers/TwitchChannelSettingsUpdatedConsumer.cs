using Hamdlebot.Models;
using Hamdlebot.Models.Enums;
using HamdleBot.Services.Twitch;
using MassTransit;

namespace HamdleBot.Services.Consumers;

public class TwitchChannelSettingsUpdatedConsumer : IConsumer<TwitchChannelUpdateMessage>
{
    private readonly TwitchChannel _channel;

    public TwitchChannelSettingsUpdatedConsumer(TwitchChannel channel)
    {
        _channel = channel;
    }
    
    public async Task Consume(ConsumeContext<TwitchChannelUpdateMessage> context)
    {
        Console.WriteLine($"Message received: {context.Message.Action} for channel {context.Message.Channel?.TwitchChannelName}");
        switch (context.Message.Action)
        {
            case ActionType.UpdateChannel:
                _channel.UpdateChannelSettings(context.Message.Channel!);
                break;
            case ActionType.UpdateObsSettings:
                _channel.UpdateObsSettings(context.Message.ObsSettings!);
                break;
            case ActionType.ConnectToObs:
                await _channel.ConnectToObs();
                break;
            case ActionType.DisconnectFromObs:
                await _channel.DisconnectFromObs();
                break;
        }
    }
}