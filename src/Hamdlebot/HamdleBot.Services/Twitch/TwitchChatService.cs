using System.Text.Json;
using Hamdle.Cache;
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
        _cache.Subscriber.Subscribe(new RedisChannel("bot:twitch:token", RedisChannel.PatternMode.Auto)).OnMessage(
            async message =>
            {
                var token = JsonSerializer.Deserialize<ClientCredentialsTokenResponse>(message.Message!);
                await _cache.AddItem("twitchOauthToken", token!.AccessToken, TimeSpan.FromSeconds(token.ExpiresIn));
                await _cache.AddItem("twitchRefreshToken", token.RefreshToken, TimeSpan.FromDays(30));
                if (_webSocketHandler != null)
                {
                    await _webSocketHandler.Disconnect();
                }
                await CreateWebSocket(_cancellationToken!.Value);
            });
    }

    public async Task CreateWebSocket(CancellationToken token)
    {
        _cancellationToken ??= token;
        
        await InsertValidCommands();
        _webSocketHandler ??= new TwitchChatWebSocketHandler("wss://irc-ws.chat.twitch.tv:443", _cancellationToken.Value,
            _logClient, "hamhamreborn");
        
        var tokenResponse = await Authenticate();

        if (tokenResponse == null)
        {
            await _logClient.LogMessage(new LogMessage("Failed to authenticate with Twitch. No valid token found.", DateTime.UtcNow, SeverityLevel.Error));
            return;
        }
        
        _webSocketHandler.MessageReceived += async message =>
        {
            if (message.Message.Contains("PING"))
            {
                await _logClient.LogMessage(new LogMessage("PING received from Twitch.", DateTime.UtcNow, SeverityLevel.Info));
                await _webSocketHandler.SendNonChatMessage("PONG :tmi.twitch.tv");
                await _logClient.LogMessage(new LogMessage("PONG :tmi.twitch.tv sent back to Twitch.", DateTime.UtcNow, SeverityLevel.Info));
            }

            if (IsSelf(message.DisplayName))
            {
                return;
            }
            
            if (_hamdleService.IsHamdleVotingInProgress())
            {
                if (int.TryParse(message.Message, out var vote))
                {
                    _hamdleService.SubmitVoteForGuess(message.DisplayName, vote);
                }
            }

            if (!_hamdleService.IsHamdleVotingInProgress()
                && _hamdleService.IsHamdleSessionInProgress())
            {
                await _hamdleService.SubmitGuess(message.User, message.Message);
            }
            else
            {
                if (message.Message.Contains("!#") && ShouldProcess(message.Message))
                {
                    await ProcessCommand(message.Message);
                }
            }
        };
        
        _webSocketHandler.Connected += async () =>
        {
            await OnConnected(tokenResponse.AccessToken);
        };
        
        await _webSocketHandler.Connect();

        _hamdleService.SendMessageToChat += Handle_Hamdle_Message!;
    }

    private async Task OnConnected(string accessToken)
    {
        await _logClient.LogMessage(new LogMessage("Connecting to Twitch Chat.", DateTime.UtcNow, SeverityLevel.Info));
        await _logClient.LogMessage(new LogMessage("Connection to Twitch Chat Successful.", DateTime.UtcNow, SeverityLevel.Info));
        var capReq = $"CAP REQ :twitch.tv/membership twitch.tv/tags twitch.tv/commands";
        var pass = $"PASS oauth:{accessToken}";
        var nick = "NICK hamdlebot";
        await _webSocketHandler!.SendNonChatMessage(pass);
        await _webSocketHandler.SendNonChatMessage(nick);
        await _webSocketHandler.SendNonChatMessage(capReq);
        await Task.Delay(3000);
        await _webSocketHandler.SendNonChatMessage("JOIN #hamhamReborn");
        await _webSocketHandler.SendMessageToChat("hamdlebot has arrived Kappa");
    }
    
    private async Task<ClientCredentialsTokenResponse?> Authenticate()
    {
        ClientCredentialsTokenResponse tokenResponse;
        var oauthToken = await _cache.GetItem("twitchOauthToken");
        var refreshToken = await _cache.GetItem("twitchRefreshToken");

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
        await _cache.AddItem("twitchOauthToken", tokenResponse.AccessToken, TimeSpan.FromSeconds(tokenResponse.ExpiresIn));
        await _cache.AddItem("twitchRefreshToken", tokenResponse.RefreshToken, TimeSpan.FromDays(30));
        
        return tokenResponse;
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
                await _cache.Subscriber.PublishAsync(new RedisChannel("startHamdleScene", RedisChannel.PatternMode.Auto), "start");
                break;
        }
    }
}