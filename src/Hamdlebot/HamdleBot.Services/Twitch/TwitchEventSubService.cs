using Hamdle.Cache;
using Hamdlebot.Core;
using Hamdlebot.Core.Models.Enums;
using HamdleBot.Services.Factories;
using HamdleBot.Services.Handlers;
using HamdleBot.Services.Twitch.Interfaces;
using Microsoft.Extensions.Options;

namespace HamdleBot.Services.Twitch;

public class TwitchEventSubService : ITwitchEventSubService
{
    private const byte KeepaliveSeconds = 60;
    private readonly IOptions<AppConfigSettings> _appConfigSettings;
    private readonly ICacheService _cacheService;
    private TwitchEventSubWebSocketHandler _eventSubHandler;
    public TwitchEventSubService(IOptions<AppConfigSettings> appConfigSettings, ICacheService cacheService)
    {
        _appConfigSettings = appConfigSettings;
        _cacheService = cacheService;
    }
    
    public async Task StartSubscriptions(string channelName, CancellationToken cancellationToken)
    {
        var twitchSettings = _appConfigSettings.Value.TwitchConnectionInfo; 
        var authToken = await _cacheService.GetItem(CacheKeyType.TwitchOauthToken);
        if (authToken == null)
        {
            // dont do this.
            throw new Exception("Twitch OAuth token not found in cache.");
        }
        var twitchApi =
            TwitchApiServiceFactory.CreateTwitchApiService(authToken!, twitchSettings!.ClientId!, cancellationToken);
        var users = await twitchApi.GetUsersByLogin([channelName, "hamdlebot"]);
        var channelUser = users?.Data.FirstOrDefault(x => x.DisplayName.Equals(channelName, StringComparison.CurrentCultureIgnoreCase));
        var hamdlebot = users?.Data.FirstOrDefault(x => x.DisplayName.Equals("hamdlebot", StringComparison.CurrentCultureIgnoreCase));
        if (users != null)
        {
            _eventSubHandler = new TwitchEventSubWebSocketHandler(
                $"wss://eventsub.wss.twitch.tv/ws?keepalive_timeout_seconds={KeepaliveSeconds}",
                channelUser!.Id,
                hamdlebot!.Id,
                cancellationToken,
                3,
                authToken,
                twitchSettings.ClientId!,
                [SubscriptionType.StreamOnline, SubscriptionType.StreamOffline]
            );
            _eventSubHandler.OnChatChannelMessage += message =>
            {
                Console.WriteLine(message.Metadata.MessageId);
            };
            _eventSubHandler.OnStreamOffline += message =>
            {
                Console.WriteLine(message.Metadata.MessageId);
            };
            await _eventSubHandler.StartEventSubscriptions();
        }
        
    }
}