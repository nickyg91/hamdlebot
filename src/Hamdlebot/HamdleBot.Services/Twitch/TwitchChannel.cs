using System.Net.WebSockets;
using Hamdle.Cache;
using Hamdlebot.Core;
using Hamdlebot.Core.Extensions;
using Hamdlebot.Data.Contexts.Hamdlebot.Entities;
using HamdleBot.Services.Handlers;
using HamdleBot.Services.OBS;

namespace HamdleBot.Services.Twitch;

public class TwitchChannel : IObserver<string>
{
    private readonly BotChannel _botChannel;
    private readonly TwitchChatWebSocketHandler _webSocketHandler;
    private ObsWebSocketHandler? _obsWebSocketHandler;
    private string _botAccessToken;
    private readonly ICacheService _cacheService;
    private readonly CancellationToken _cancellationToken;
    //private HamdleContext? _hamdleContext;

    private readonly List<string> _baseChannelCommands =
    [
        "!#commands",
        "!#random",
        "!#hamdle"
    ];
    
    public TwitchChannel(
        BotChannel channel,
        string url,
        string botAccessToken,
        ICacheService cacheService,
        CancellationToken cancellationToken)
    {
        _botAccessToken = botAccessToken;
        _cacheService = cacheService;
        _botChannel = channel;
        _webSocketHandler =
            new TwitchChatWebSocketHandler
            (
                url,
                cancellationToken,
                _botChannel.TwitchChannelName,
                3
            );
        SetupEvents();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);
        _cancellationToken = cts.Token;
    }

    public async Task LeaveChannel()
    {
        await _webSocketHandler.SendMessageToChat("I'm leaving the channel. Bye!");
        await _webSocketHandler.SendPartMessage();
        await _webSocketHandler.Disconnect();
    }

    public void Connect()
    {
        if (_webSocketHandler.State != WebSocketState.Open 
            && _webSocketHandler.State != WebSocketState.Connecting)
        {
            _ = Task.Run(async () => await _webSocketHandler.Connect());
        }
    }
    
    public void OnCompleted()
    {
        //throw new NotImplementedException();
    }

    public void OnError(Exception error)
    {
        //throw new NotImplementedException();
    }

    public void OnNext(string botAccessToken)
    {
        _ = Task.Run(async () => await Reauthenticate(botAccessToken));
    }

    public async Task ConnectToObs()
    {
        if (_botChannel.AllowAccessToObs)
        {
            var obsDetails = await _cacheService.GetObject<ObsSettings>($"{CacheKeyType.UserObsSettings}:{_botChannel.TwitchUserId}");
            if (obsDetails != null && _obsWebSocketHandler == null)
            {
                _obsWebSocketHandler = new ObsWebSocketHandler(obsDetails!, _cancellationToken, 3);
            }

            if (_obsWebSocketHandler != null && _obsWebSocketHandler.State != WebSocketState.Open
                                             && _obsWebSocketHandler.State != WebSocketState.Connecting)
            {
                await _obsWebSocketHandler.Connect();
            }
        }
    }
    
    private void SetupEvents()
    {
        _webSocketHandler.MessageReceived += async message => { await OnMessageReceived(message); };
        _webSocketHandler.Connected += async () =>
        {
            await Authenticate();
            await _webSocketHandler.SendMessageToChat("Never fear, hamdlebot is here!");
        };
    }

    private async Task Authenticate()
    {
        await AuthenticateInternal();
    }
    
    private async Task Reauthenticate(string botAccessToken)
    {
        _botAccessToken = botAccessToken;
        await AuthenticateInternal();
    }

    private async Task AuthenticateInternal()
    {
        var capReq = $"CAP REQ :twitch.tv/membership twitch.tv/tags twitch.tv/commands";
        var pass = $"PASS oauth:{_botAccessToken}";
        var nick = "NICK hamdlebot";
        await _webSocketHandler.SendMessage(pass);
        await _webSocketHandler.SendMessage(nick);
        await _webSocketHandler.SendMessage(capReq);
        await Task.Delay(3000);
        await _webSocketHandler.JoinChannel();
    }
    
    private async Task OnMessageReceived(string message)
    {
        if (message.IsPingMessage())
        {
            await _webSocketHandler.SendMessage("PONG :tmi.twitch.tv");
        }
        
        var ircMessage = message.ToTwitchMessage();
        if (ircMessage.IsBot())
        {
            return;
        }
        
        if (ircMessage.IsCommand)
        {
            if (_baseChannelCommands.Contains(ircMessage.Message!))
            {
                switch (ircMessage.Message)
                {
                    case "!#commands":
                        await _webSocketHandler.SendMessageToChat("Commands: !#commands, !#random, !#hamdle");
                        break;
                    case "!#hamdle":
                        if (_botChannel.IsHamdleEnabled)
                        {
                            // start a hamdle context
                        }
                        else
                        {
                            await _webSocketHandler.SendMessageToChat("Hamdle is not enabled in this channel.");                                
                        }
                        break;
                }

                return;
            }
            var command = _botChannel.BotChannelCommands.FirstOrDefault(x => x.Command == ircMessage.Message);
            if (command != null)
            {
                await _webSocketHandler.SendMessageToChat(command.Response);
                return;
            }
            await _webSocketHandler!.SendMessageToChat("Invalid command! SirSad");
        }
    }
}