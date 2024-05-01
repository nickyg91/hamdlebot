using System.Text.Json;
using Hamdle.Cache;
using Hamdlebot.Core.Extensions;
using Hamdlebot.Core.Models.Logging;
using Hamdlebot.Core.SignalR.Clients.Logging;
using Hamdlebot.Models;
using HamdleBot.Services.Handlers;
using HamdleBot.Services.Twitch.Interfaces;
using StackExchange.Redis;

namespace HamdleBot.Services.Twitch;

public class TwitchChatService : ITwitchChatService
{
    private readonly ICacheService _cache;
    private readonly IWordService _wordService;
    private readonly IHamdleService _hamdleService;
    private TwitchChatWebSocketHandler? _webSocketHandler;
    private readonly ITwitchIdentityApiService _identityApiService;
    private readonly IBotLogClient _logClient;
    private CancellationToken? _cancellationToken;
    private readonly RedisChannel _botTokenChannel;
    private readonly RedisChannel _startHamdleSceneChannel;
    public TwitchChatService(
        IWordService wordService, 
        ICacheService cache,
        IHamdleService hamdleService,
        ITwitchIdentityApiService identityApiService,
        IBotLogClient logClient)
    {
        _wordService = wordService;
        _cache = cache;
        _hamdleService = hamdleService;
        _identityApiService = identityApiService;
        _logClient = logClient;
        _botTokenChannel = new RedisChannel(RedisChannelType.BotTwitchToken, RedisChannel.PatternMode.Auto);
        _startHamdleSceneChannel = new RedisChannel(RedisChannelType.StartHamdleScene, RedisChannel.PatternMode.Auto);
        SetupSubscriptions();
    }

    public async Task CreateWebSocket(CancellationToken cancellationToken)
    {
        _cancellationToken ??= cancellationToken;
        
        await InsertValidCommands();
        _webSocketHandler ??= new TwitchChatWebSocketHandler("wss://irc-ws.chat.twitch.tv:443", _cancellationToken.Value, "hamhamreborn");
        
        var tokenResponse = await Authenticate();

        if (tokenResponse == null)
        {
            await _logClient.LogMessage(new LogMessage("Failed to authenticate with Twitch. No valid token found.", DateTime.UtcNow, SeverityLevel.Error));
            return;
        }
        
        _webSocketHandler.MessageReceived += async message =>
        {
            var ircMessage = message.ToTwitchMessage();
            if (ircMessage.Message!.Contains("PING"))
            {
                await _logClient.LogMessage(new LogMessage("PING received from Twitch.", DateTime.UtcNow, SeverityLevel.Info));
                await _webSocketHandler.SendMessageToChat("PONG :tmi.twitch.tv");
                await _logClient.LogMessage(new LogMessage("PONG :tmi.twitch.tv sent back to Twitch.", DateTime.UtcNow, SeverityLevel.Info));
            }

            if (IsSelf(ircMessage.DisplayName!))
            {
                return;
            }
            
            if (_hamdleService.IsHamdleVotingInProgress())
            {
                if (int.TryParse(ircMessage.Message, out var vote))
                {
                    _hamdleService.SubmitVoteForGuess(ircMessage.DisplayName!, vote);
                }
            }

            if (!_hamdleService.IsHamdleVotingInProgress()
                && _hamdleService.IsHamdleSessionInProgress())
            {
                await _hamdleService.SubmitGuess(ircMessage.User!, ircMessage.Message);
            }
            else
            {
                if (ircMessage.Message.Contains("!#") && ShouldProcess(ircMessage.Message))
                {
                    await ProcessCommand(ircMessage.Message);
                }
            }
        };
        
        _webSocketHandler.Connected += async () =>
        {
            await OnConnected(tokenResponse.AccessToken);
        };
        
        _webSocketHandler.ReconnectStarted += async () =>
        {
            await _logClient.LogMessage(new LogMessage("Reconnecting to Twitch Chat.", DateTime.UtcNow, SeverityLevel.Info));
        };
        
        await _webSocketHandler.Connect();

        _hamdleService.SendMessageToChat += Handle_Hamdle_Message!;
    }

    private async Task OnConnected(string accessToken)
    {
        await _logClient.LogMessage(new LogMessage("Connecting to Twitch Chat.", DateTime.UtcNow, SeverityLevel.Info));
        await _logClient.LogMessage(new LogMessage("Connection to Twitch Chat Successful.", DateTime.UtcNow, SeverityLevel.Info));
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
            await _logClient.LogMessage(new LogMessage("Valid OAuth token found.", DateTime.UtcNow, SeverityLevel.Info));
            return new ClientCredentialsTokenResponse
            {
                AccessToken = oauthToken,
                RefreshToken = refreshToken
            };
        }
        
        if (oauthToken == null || refreshToken == null)
        {
            await _logClient.LogMessage(new LogMessage("Valid OAuth token not found.", DateTime.UtcNow, SeverityLevel.Info));
            return null;
        }

        await _logClient.LogMessage(new LogMessage("Fetching new twitch OAuthToken.", DateTime.UtcNow, SeverityLevel.Info));
        tokenResponse = await _identityApiService.RefreshToken(refreshToken);
        await _cache.AddItem(CacheKeyType.TwitchOauthToken, tokenResponse.AccessToken, TimeSpan.FromSeconds(tokenResponse.ExpiresIn));
        await _cache.AddItem(CacheKeyType.TwitchRefreshToken, tokenResponse.RefreshToken, TimeSpan.FromDays(30));
        
        return tokenResponse;
    }

    private void SetupSubscriptions()
    {
        _cache.Subscriber.Subscribe(_botTokenChannel).OnMessage(
            async message =>
            {
                var token = JsonSerializer.Deserialize<ClientCredentialsTokenResponse>(message.Message!);
                await _cache.AddItem(CacheKeyType.TwitchOauthToken, token!.AccessToken, TimeSpan.FromSeconds(token.ExpiresIn));
                await _cache.AddItem(CacheKeyType.TwitchRefreshToken, token.RefreshToken, TimeSpan.FromDays(30));
                if (_webSocketHandler != null)
                {
                    await _webSocketHandler.Disconnect();
                }
                await CreateWebSocket(_cancellationToken!.Value);
            });
    }
    
    private async void Handle_Hamdle_Message(object sender, string message)
    {
        await _webSocketHandler!.SendMessageToChat(message);
    }

    private static bool ShouldProcess(string command)
    {
        return command.StartsWith("!#");
    }

    private bool IsSelf(string message)
    {
        var parsed = string.Join("", message.TakeWhile(x => x != '!')).Replace(":", "");
        return parsed == "hamdlebot" || parsed == "nightbot";
    }
    
    private async Task InsertValidCommands()
    {
        var validCommands = new List<string>
        {
            "!#commands",
            "!#random",
            "!#hamdle"
        };
        foreach (var command in validCommands)
        {
            await _cache.AddToSet("commands", command);
        }
    }

    private async Task<bool> IsValidCommand(string command)
    {
        return await _cache.ContainsMember("commands", command);
    }

    private async Task ProcessCommand(string command)
    {
        var isValidCommand = await IsValidCommand(command);
        if (!isValidCommand)
        {
            await _webSocketHandler!.SendMessageToChat("Invalid command! SirSad");
        }

        switch (command)
        {
            case "!#commands":
                await _webSocketHandler!.SendMessageToChat("Commands: !#<command> commands, random, hamdle");
                break;
            case "!#random":
                var word = await _wordService.GetRandomWord();
                await _webSocketHandler!.SendMessageToChat(word ?? "nooooo!");
                break;
            case "!#hamdle":
                await _cache.Subscriber.PublishAsync(_startHamdleSceneChannel, "start");
                break;
        }
    }
}