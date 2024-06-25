using Hamdle.Cache;
using Hamdlebot.Core.Extensions;
using Hamdlebot.Core.Models;
using Hamdlebot.Core.Models.Logging;
using Hamdlebot.Core.SignalR.Clients.Logging;
using Hamdlebot.Data.Contexts.Hamdlebot;
using Hamdlebot.Data.Contexts.Hamdlebot.Entities;
using Hamdlebot.Models;
using HamdleBot.Services.Handlers;
using HamdleBot.Services.Twitch.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace HamdleBot.Services.Twitch;

public class TwitchChatService : ITwitchChatService
{
    private const string TwitchWebSocketUrl = "wss://irc-ws.chat.twitch.tv:443";
    private readonly Dictionary<long, TwitchChannel> _channels = new();
    private readonly ICacheService _cache;
    private readonly IWordService _wordService;
    private readonly IHamdleService _hamdleService;
    private TwitchChatWebSocketHandler? _webSocketHandler;
    private readonly ITwitchIdentityApiService _identityApiService;
    private readonly IBotLogClient _logClient;
    private CancellationToken? _cancellationToken;
    private readonly RedisChannel _botTokenChannel;
    private readonly RedisChannel _startHamdleSceneChannel;
    private readonly TwitchAuthTokenUpdateHandler _authTokenUpdateHandler;
    private readonly IServiceProvider _serviceProvider;
    
    public TwitchChatService(
        IWordService wordService,
        ICacheService cache,
        IHamdleService hamdleService,
        ITwitchIdentityApiService identityApiService,
        IBotLogClient logClient,
        TwitchAuthTokenUpdateHandler authTokenUpdateHandler,
        IServiceProvider serviceProvider)
    {
        _wordService = wordService;
        _cache = cache;
        _hamdleService = hamdleService;
        _identityApiService = identityApiService;
        _logClient = logClient;
        _authTokenUpdateHandler = authTokenUpdateHandler;
        _serviceProvider = serviceProvider;
        _botTokenChannel = new RedisChannel(RedisChannelType.BotTwitchToken, RedisChannel.PatternMode.Auto);
        _startHamdleSceneChannel = new RedisChannel(RedisChannelType.StartHamdleScene, RedisChannel.PatternMode.Auto);
        SetupSubscriptions();
    }

    public async Task CreateWebSocket(CancellationToken cancellationToken)
    {
        _cancellationToken ??= cancellationToken;
        _webSocketHandler ??= new TwitchChatWebSocketHandler("wss://irc-ws.chat.twitch.tv:443",
            _cancellationToken.Value, "hamhamreborn", 3);

        var tokenResponse = await Authenticate();

        if (tokenResponse == null)
        {
            await _logClient.LogMessage(new LogMessage("Failed to authenticate with Twitch. No valid token found.",
                DateTime.UtcNow, SeverityLevel.Error));
            return;
        }
        
        _webSocketHandler.MessageReceived += async message =>
        {
            if (message.IsPingMessage())
            {
                await _logClient.LogMessage(new LogMessage("PING received from Twitch.", DateTime.UtcNow,
                    SeverityLevel.Info));
                await _webSocketHandler.SendMessage("PONG :tmi.twitch.tv");
                await _logClient.LogMessage(new LogMessage("PONG :tmi.twitch.tv sent back to Twitch.", DateTime.UtcNow,
                    SeverityLevel.Info));
            }

            var ircMessage = message.ToTwitchMessage();
            if (ircMessage.IsBot())
            {
                return;
            }

            if (_hamdleService.IsHamdleVotingInProgress() && int.TryParse(ircMessage.Message, out var vote))
            {
                _hamdleService.SubmitVoteForGuess(ircMessage.DisplayName!, vote);
            }

            if (ircMessage.IsCommand)
            {
                await ProcessCommand(ircMessage);
            }

            if (!_hamdleService.IsHamdleVotingInProgress()
                && _hamdleService.IsHamdleSessionInProgress()
                && ircMessage.Message is not null)
            {
                await _hamdleService.SubmitGuess(ircMessage.User!, ircMessage.Message);
            }
        };

        _webSocketHandler.ReconnectStarted += async () =>
        {
            await _logClient.LogMessage(new LogMessage("Reconnecting to Twitch Chat.", DateTime.UtcNow,
                SeverityLevel.Info));
        };

        await _webSocketHandler.Connect();
        _hamdleService.SendMessageToChat -= Handle_Hamdle_Message!;
        _hamdleService.SendMessageToChat += Handle_Hamdle_Message!;
    }

    public async Task JoinBotToChannel(BotChannel channel)
    {
        if (_channels.ContainsKey(channel.TwitchUserId))
        {
            return;
        }
        var oauthToken = await _cache.GetItem(CacheKeyType.TwitchOauthToken);
        if (oauthToken is null)
        {
            await _logClient.LogMessage(
                new LogMessage($"No valid OAuth token found. Cannot join channel {channel.TwitchUserId}.", 
                DateTime.UtcNow,
                SeverityLevel.Error));
            return;
        }
        var twitchChannel = 
            new TwitchChannel(channel, TwitchWebSocketUrl, oauthToken, _cache, _cancellationToken!.Value);
        twitchChannel.Connect();
        _authTokenUpdateHandler.Subscribe(twitchChannel);
        _channels.Add(channel.TwitchUserId, twitchChannel);
    }

    public async Task LeaveChannel(long twitchUserId)
    {
        if (_channels.TryGetValue(twitchUserId, out var channel))
        {
            await channel.LeaveChannel();
            _channels.Remove(twitchUserId);
        }
    }

    public async Task JoinExistingChannels()
    {
        var token = await Authenticate();
        if (token == null)
        {
            await _logClient.LogMessage(new LogMessage("Failed to authenticate with Twitch. No valid token found.",
                DateTime.UtcNow, SeverityLevel.Error));
            return;
        }
        // for now - this is not performant AT ALL when dealing with large datasets.
        using var scope = _serviceProvider.CreateScope();
        var dbCtx = scope.ServiceProvider.GetRequiredService<HamdlebotContext>();
        var channels = await dbCtx.BotChannels.AsNoTracking().ToListAsync();
        foreach (var channel in channels)
        {
            await JoinBotToChannel(channel);
        }
    }

    public async Task ConnectToObs(long twitchUserId)
    {
        if (_channels.TryGetValue(twitchUserId, out var channel))
        {
            await channel.ConnectToObs();
        }
    }

    public async Task DisconnectFromObs(long twitchUserId)
    {
        if (_channels.TryGetValue(twitchUserId, out var channel))
        {
            await channel.DisconnectFromObs();
        }
    }

    public void SetCancellationToken(CancellationToken token)
    {
        _cancellationToken = token;
    }
    
    private async Task<ClientCredentialsTokenResponse?> Authenticate()
    {
        ClientCredentialsTokenResponse tokenResponse;
        var oauthToken = await _cache.GetItem(CacheKeyType.TwitchOauthToken);
        var refreshToken = await _cache.GetItem(CacheKeyType.TwitchRefreshToken);
        
        if (oauthToken != null && refreshToken != null)
        {
            await _logClient.LogMessage(new LogMessage("Valid OAuth token found.", DateTime.UtcNow,
                SeverityLevel.Info));
            return new ClientCredentialsTokenResponse
            {
                AccessToken = oauthToken,
                RefreshToken = refreshToken
            };
        }

        if (oauthToken == null || refreshToken == null)
        {
            await _logClient.LogMessage(new LogMessage("Valid OAuth token not found.", DateTime.UtcNow,
                SeverityLevel.Info));
            return null;
        }

        await _logClient.LogMessage(new LogMessage("Fetching new twitch OAuthToken.", DateTime.UtcNow,
            SeverityLevel.Info));
        tokenResponse = await _identityApiService.RefreshToken(refreshToken);
        await _cache.AddItem(CacheKeyType.TwitchOauthToken, tokenResponse.AccessToken,
            TimeSpan.FromSeconds(tokenResponse.ExpiresIn));
        await _cache.AddItem(CacheKeyType.TwitchRefreshToken, tokenResponse.RefreshToken, TimeSpan.FromDays(30));
        _authTokenUpdateHandler.UpdateToken(tokenResponse.AccessToken);
        return tokenResponse;
    }

    private void SetupSubscriptions()
    {
        _cache.Subscriber.Subscribe(_botTokenChannel).OnMessage(
            async _ =>
            {
                await Authenticate();
            });
    }

    private async void Handle_Hamdle_Message(object sender, string message)
    {
        await _webSocketHandler!.SendMessageToChat(message);
    }

    private async Task ProcessCommand(TwitchMessage message)
    {
        switch (message.Message)
        {
            case "!#commands":
                await _webSocketHandler!.SendMessageToChat("Commands: !#<command> commands, random, hamdle, guess");
                break;
            case "!#random":
                var word = await _wordService.GetRandomWord();
                await _webSocketHandler!.SendMessageToChat(word ?? "nooooo!");
                break;
            case "!#hamdle":
                if (!_hamdleService.IsHamdleSessionInProgress())
                {
                    await _cache.Subscriber.PublishAsync(_startHamdleSceneChannel, "start");
                }
                else
                {
                    await _webSocketHandler!.SendMessageToChat("Hamdle is already in progress. Can't start another!");
                }
                break;
        }
    }
}