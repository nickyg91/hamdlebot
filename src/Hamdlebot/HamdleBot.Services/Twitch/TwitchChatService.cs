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
    private readonly Dictionary<int, TwitchChannel> _channels = new();
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
    
    private readonly List<string> _validCommands =
    [
        "!#commands",
        "!#random",
        "!#hamdle"
    ];

    public TwitchChatService(
        IWordService wordService,
        ICacheService cache,
        IHamdleService hamdleService,
        ITwitchIdentityApiService identityApiService,
        IBotLogClient logClient,
        TwitchAuthTokenUpdateHandler authTokenUpdateHandler,
        HamdlebotContext dbContext, IServiceProvider serviceProvider)
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
        await InsertValidCommands();
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

        _webSocketHandler.Connected += async () => { await OnConnected(tokenResponse.AccessToken); };

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
        if (_channels.ContainsKey(channel.ChannelId))
        {
            return;
        }
        var oauthToken = await _cache.GetItem(CacheKeyType.TwitchOauthToken);
        if (oauthToken is null)
        {
            await _logClient.LogMessage(
                new LogMessage($"No valid OAuth token found. Cannot join channel {channel.TwitchChannelName}.", 
                DateTime.UtcNow,
                SeverityLevel.Error));
            return;
        }
        var twitchChannel = new TwitchChannel(channel, oauthToken!, "wss://irc-ws.chat.twitch.tv:443",  _cancellationToken!.Value);
        await twitchChannel.Authenticate();
        _authTokenUpdateHandler.Subscribe(twitchChannel);
        _channels.Add(channel.ChannelId, twitchChannel);
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
        _authTokenUpdateHandler.UpdateToken(token.AccessToken);
        // for now - this is not performant AT ALL when dealing with large datasets.
        using var scope = _serviceProvider.CreateScope();
        var dbCtx = scope.ServiceProvider.GetRequiredService<HamdlebotContext>();
        var channels = await dbCtx.BotChannels.ToListAsync();
        foreach (var channel in channels)
        {
            await JoinBotToChannel(channel);
        }
    }

    private async Task OnConnected(string accessToken)
    {
        await _logClient.LogMessage(new LogMessage("Connecting to Twitch Chat.", DateTime.UtcNow, SeverityLevel.Info));
        await _logClient.LogMessage(new LogMessage("Connection to Twitch Chat Successful.", DateTime.UtcNow,
            SeverityLevel.Info));
        await _logClient.SendBotStatus(BotStatusType.Online);
        var capReq = $"CAP REQ :twitch.tv/membership twitch.tv/tags twitch.tv/commands";
        var pass = $"PASS oauth:{accessToken}";
        var nick = "NICK hamdlebot";
        await _webSocketHandler!.SendMessage(pass);
        await _webSocketHandler.SendMessage(nick);
        await _webSocketHandler.SendMessage(capReq);
        await Task.Delay(3000);
        await _webSocketHandler.JoinChannel();
        await _webSocketHandler.SendMessageToChat("hamdlebot has arrived Kappa");
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

    private async Task InsertValidCommands()
    {
        foreach (var command in _validCommands)
        {
            await _cache.AddToSet(CacheKeyType.BotCommands, command);
        }
    }

    private async Task ProcessCommand(TwitchMessage message)
    {
        if (!message.IsValidCommand(_validCommands))
        {
            await _webSocketHandler!.SendMessageToChat("Invalid command! SirSad");
        }

        switch (message.Message)
        {
            case "!#commands":
                await _webSocketHandler!.SendMessageToChat("Commands: !#<command> commands, random, hamdle");
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