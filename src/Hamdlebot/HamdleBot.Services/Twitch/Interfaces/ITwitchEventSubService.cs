namespace HamdleBot.Services.Twitch.Interfaces;

public interface ITwitchEventSubService
{
    Task StartSubscriptions(string channelName, CancellationToken cancellationToken);
}