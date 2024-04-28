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
    private WebSocketHandler? _webSocketHandler;
    private readonly ITwitchIdentityApiService _identityApiService;
    private readonly IBotLogClient _logClient;

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
    }

    public async Task CreateWebSocket(CancellationToken token)
    {
        await InsertValidCommands();
        _webSocketHandler = new WebSocketHandler("wss://irc-ws.chat.twitch.tv:443", token, _logClient);
        
        var tokenResponse = await Authenticate();
        await _webSocketHandler.Connect();
        
        _webSocketHandler.MessageReceived += async message =>
        {
            if (message.Message.Contains("PING"))
            {
                await _logClient.LogMessage(new LogMessage("PING received from Twitch.", DateTime.UtcNow, SeverityLevel.Info));
                await _webSocketHandler.SendMessage("PONG :tmi.twitch.tv");
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

        _hamdleService.SendMessageToChat += Handle_Hamdle_Message!;
    }

    private async Task OnConnected(string accessToken)
    {
        await _logClient.LogMessage(new LogMessage("Connecting to Twitch Chat.", DateTime.UtcNow, SeverityLevel.Info));
        await _webSocketHandler!.SendMessage("JOIN #hamhamReborn");
        await _webSocketHandler.SendMessage("hamdlebot has arrived Kappa");
        await _logClient.LogMessage(new LogMessage("Connection to Twitch Chat Successful.", DateTime.UtcNow, SeverityLevel.Info));
        var capReq = $"CAP REQ :twitch.tv/membership twitch.tv/tags twitch.tv/commands";
        var pass = $"PASS oauth:{accessToken}";
        var nick = "NICK hamdlebot";
        await _webSocketHandler.SendMessage(pass);
        await _webSocketHandler.SendMessage(nick);
        await _webSocketHandler.SendMessage(capReq);
    }
    
    private async Task<ClientCredentialsTokenResponse> Authenticate()
    {
        ClientCredentialsTokenResponse tokenResponse;
        var oauthToken = await _cache.GetItem("twitchOauthToken");
        var refreshToken = await _cache.GetItem("twitchRefreshToken");

        if (oauthToken != null)
        {
            await _logClient.LogMessage(new LogMessage("Valid OAuth token found.", DateTime.UtcNow, SeverityLevel.Info));
            return new ClientCredentialsTokenResponse
            {
                AccessToken = oauthToken
            };
        }
        
        if (oauthToken == null && refreshToken == null)
        {
            await _logClient.LogMessage(new LogMessage("Valid OAuth token not found.", DateTime.UtcNow, SeverityLevel.Info));
            tokenResponse = await _identityApiService.GetTokenFromCodeFlow();
            await _cache.AddItem("twitchOauthToken", tokenResponse.AccessToken, TimeSpan.FromSeconds(tokenResponse.ExpiresIn));
            await _cache.AddItem("twitchRefreshToken", tokenResponse.RefreshToken, TimeSpan.FromDays(30));
            return tokenResponse;
        }

        if (refreshToken == null)
        {
            throw new Exception("An error occurred while authenticating with Twitch.");
        }
        await _logClient.LogMessage(new LogMessage("Fetching new twitch OAuthToken.", DateTime.UtcNow, SeverityLevel.Info));
        tokenResponse = await _identityApiService.RefreshToken(refreshToken);
        await _cache.AddItem("twitchOauthToken", tokenResponse.AccessToken, TimeSpan.FromSeconds(tokenResponse.ExpiresIn));
        await _cache.AddItem("twitchRefreshToken", tokenResponse.RefreshToken, TimeSpan.FromDays(30));
        
        return tokenResponse;
    }
    
    private async void Handle_Hamdle_Message(object sender, string message)
    {
        await _webSocketHandler!.SendMessage(message);
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
            await _webSocketHandler!.SendMessage("Invalid command! SirSad");
        }

        switch (command)
        {
            case "!#commands":
                await _webSocketHandler!.SendMessage("Commands: !#<command> commands, random, hamdle");
                break;
            case "!#random":
                var word = await _wordService.GetRandomWord();
                await _webSocketHandler!.SendMessage(word ?? "nooooo!");
                break;
            case "!#hamdle":
                await _cache.Subscriber.PublishAsync(new RedisChannel("startHamdleScene", RedisChannel.PatternMode.Auto), "start");
                break;
        }
    }
}