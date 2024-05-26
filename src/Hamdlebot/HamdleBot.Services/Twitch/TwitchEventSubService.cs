using System.Text.Json;
using Hamdle.Cache;
using Hamdlebot.Core;
using Hamdlebot.Core.Models.Enums;
using Hamdlebot.Core.Models.Logging;
using Hamdlebot.Core.SignalR.Clients.Logging;
using Hamdlebot.Models;
using HamdleBot.Services.Factories;
using HamdleBot.Services.Handlers;
using HamdleBot.Services.Twitch.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HamdleBot.Services.Twitch;

public class TwitchEventSubService : ITwitchEventSubService
{
    private const byte KeepaliveSeconds = 60;
    private readonly IOptions<AppConfigSettings> _appConfigSettings;
    private readonly ICacheService _cacheService;
    private readonly RedisChannel _botTokenChannel;
    private readonly IBotLogClient _logClient;
    private readonly ILogger<TwitchEventSubService> _logger;
    private TwitchEventSubWebSocketHandler? _eventSubHandler;
    private CancellationToken? _cancellationToken;

    public TwitchEventSubService(IOptions<AppConfigSettings> appConfigSettings, ICacheService cacheService, IBotLogClient logClient, ILogger<TwitchEventSubService> logger)
    {
        _appConfigSettings = appConfigSettings;
        _cacheService = cacheService;
        _logClient = logClient;
        _logger = logger;
        _botTokenChannel = new RedisChannel(RedisChannelType.BotTwitchToken, RedisChannel.PatternMode.Auto);
        SetupSubscriptions();
    }

    public async Task StartSubscriptions(string channelName, CancellationToken cancellationToken)
    {
        var twitchSettings = _appConfigSettings.Value.TwitchConnectionInfo;
        var authToken = await _cacheService.GetItem(CacheKeyType.TwitchOauthToken);
        _cancellationToken = cancellationToken;
        
        if (authToken is null)
        {
            _logger.LogError("Auth token is null. Cannot start subscriptions.");
            return;
        }

        var twitchApi =
            TwitchApiServiceFactory.CreateTwitchApiService(authToken!, twitchSettings!.ClientId!, cancellationToken);
        
        var users = await twitchApi.GetUsersByLogin([channelName, "hamdlebot"]);
        var channelUser = users?.Data.FirstOrDefault(x =>
            x.DisplayName.Equals(channelName, StringComparison.CurrentCultureIgnoreCase));
        var hamdlebot = users?.Data.FirstOrDefault(x =>
            x.DisplayName.Equals("hamdlebot", StringComparison.CurrentCultureIgnoreCase));
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

            SetupEventSubHandlerEvents();
            
            await _eventSubHandler.StartEventSubscriptions();
        }
    }

    private void SetupSubscriptions()
    {
        _cacheService.Subscriber.Subscribe(_botTokenChannel).OnMessage(
            async message =>
            {
                var token = JsonSerializer.Deserialize<ClientCredentialsTokenResponse>(message.Message!);
                if (_eventSubHandler is not null)
                {
                    await _eventSubHandler.Disconnect();
                    _eventSubHandler.SetNewAuthToken(token!.AccessToken);
                    _logger.LogInformation("Starting new eventsub connection.");
                    await StartSubscriptions("hamhamreborn", _cancellationToken!.Value);
                }
            });
    }

    private void SetupEventSubHandlerEvents()
    {
        if (_eventSubHandler is null)
        {
            return;
        }
        _eventSubHandler.OnWelcomeMessage += _ =>
        {
            _logClient.LogMessage(new LogMessage("EventSub Connection Established", DateTime.UtcNow, SeverityLevel.Info));
        };
        _eventSubHandler.OnStreamOffline += _ =>
        {
            _cacheService.AddItem(CacheKeyType.IsStreamOnline, "false");
        };
        _eventSubHandler.OnStreamOnline += _ =>
        {
            _cacheService.AddItem(CacheKeyType.IsStreamOnline, "true");
        };
        _eventSubHandler.OnKeepaliveMessage += message =>
        {
            _logClient.LogMessage(new LogMessage($"Keepalive Message Received: {message.Metadata.MessageId}", DateTime.UtcNow, SeverityLevel.Info));
        };
    }
}