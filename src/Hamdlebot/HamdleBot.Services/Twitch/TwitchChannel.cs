using System.Net.WebSockets;
using System.Timers;
using Hamdle.Cache;
using Hamdlebot.Core;
using Hamdlebot.Core.Extensions;
using Hamdlebot.Core.Models.Enums;
using Hamdlebot.Models.ViewModels;
using HamdleBot.Services.Hamdle;
using HamdleBot.Services.Handlers;
using Microsoft.AspNetCore.SignalR.Client;

namespace HamdleBot.Services.Twitch;

public class TwitchChannel
{
    private Channel _botChannel;
    private readonly TwitchChatWebSocketHandler _webSocketHandler;
    private ObsWebSocketHandler? _obsWebSocketHandler;
    private string _botAccessToken;
    private readonly ICacheService _cacheService;
    private readonly HubConnection _hamdleHubConnection;
    private readonly HubConnection _channelNotificationsConnection;
    private readonly CancellationToken _cancellationToken;
    private HamdleContext? _hamdleContext;
    private HashSet<string> _hamdleWords = [];
    private ObsSettings? _obsSettings;
    private readonly System.Timers.Timer _statusPingTimer = new(30000)
    {
        AutoReset = true
    };
    private readonly System.Timers.Timer _statusObsPingTimer = new(30000)
    {
        AutoReset = true
    };
    public bool IsConnected => _webSocketHandler.State == WebSocketState.Open
                               && _webSocketHandler.State != WebSocketState.Connecting;

    private readonly List<string> _baseChannelCommands =
    [
        "!#commands",
        "!#random",
        "!#hamdle",
        "!#guess"
    ];

    public TwitchChannel(
        Channel channel,
        string url,
        string botAccessToken,
        ObsSettings? obsSettings,
        ICacheService cacheService,
        HubConnection hamdleHubConnection,
        HubConnection channelNotificationsConnection,
        CancellationToken cancellationToken)
    {
        _botAccessToken = botAccessToken;
        _cacheService = cacheService;
        _hamdleHubConnection = hamdleHubConnection;
        _channelNotificationsConnection = channelNotificationsConnection;
        _botChannel = channel;
        _webSocketHandler =
            new TwitchChatWebSocketHandler
            (
                url,
                cancellationToken,
                _botChannel.TwitchChannelName,
                3
            );
        SetupTwitchIrcSocketEvents();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);
        _cancellationToken = cts.Token;
        _obsSettings = obsSettings;
    }

    public async Task LeaveChannel()
    {
        await _webSocketHandler.SendMessageToChat("I'm leaving the channel. Bye!");
        await _webSocketHandler.SendPartMessage();
        await _webSocketHandler.Disconnect();
    }

    public async Task Connect()
    {
        if (_webSocketHandler.State != WebSocketState.Open
            && _webSocketHandler.State != WebSocketState.Connecting)
        {
            await Task.Run(async () => await _webSocketHandler.Connect(), _cancellationToken);
        }
    }

    public async Task ConnectToObs()
    {
        try
        {
            if (_botChannel.AllowAccessToObs)
            {
                if (_obsSettings != null)
                {
                    var words = await _cacheService.GetItemsInSet(CacheKeyType.HamdleWords);
                    if (words.Count > 0)
                    {
                        _hamdleWords = words.ToHashSet();
                    }

                    _obsWebSocketHandler?.Disconnect();
                    _obsWebSocketHandler = new ObsWebSocketHandler(_obsSettings, _cancellationToken, 3);
                }

                if (_obsWebSocketHandler != null && _obsWebSocketHandler.State != WebSocketState.Open
                                                 && _obsWebSocketHandler.State != WebSocketState.Connecting)
                {
                    SetupObsSocketEvents();
                    await _obsWebSocketHandler.Connect();
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task DisconnectFromObs()
    {
        if (_obsWebSocketHandler != null)
        {
            await _obsWebSocketHandler.Disconnect();
        }
    }

    public void UpdateChannelSettings(Channel channel)
    {
        _botChannel = channel;
    }

    public void UpdateObsSettings(ObsSettings obsSettings)
    {
        _obsSettings = obsSettings;
    }

    private void SetupTwitchIrcSocketEvents()
    {
        _webSocketHandler.MessageReceived += async message => { await OnMessageReceived(message); };
        _webSocketHandler.Connected += async () =>
        {
            await Authenticate();
            await _webSocketHandler.SendMessageToChat("Never fear, hamdlebot is here!");
            await _channelNotificationsConnection.InvokeAsync("SendChannelConnectionStatus",
                ChannelConnectionStatusType.Connected, _botChannel.TwitchUserId.ToString(), _cancellationToken);
        };
        _webSocketHandler.OnDisconnect += async () =>
        {
            await _channelNotificationsConnection.InvokeAsync("SendChannelConnectionStatus",
                ChannelConnectionStatusType.Disconnected, _botChannel.TwitchUserId, _cancellationToken);
        };
        _webSocketHandler.OnFault += async () =>
        {
            await _channelNotificationsConnection.InvokeAsync("SendChannelConnectionStatus",
                ChannelConnectionStatusType.Errored, _botChannel.TwitchUserId, _cancellationToken);
        };
        
        _statusPingTimer.Elapsed += async (_, _) =>
        {
            var connectionStatus = _webSocketHandler.State switch
            {
                WebSocketState.Open => ChannelConnectionStatusType.Connected,
                WebSocketState.Aborted => ChannelConnectionStatusType.Errored,
                _ => ChannelConnectionStatusType.Disconnected
            };
            await _channelNotificationsConnection.InvokeAsync("SendChannelConnectionStatus",
                connectionStatus, _botChannel.TwitchUserId.ToString(), _cancellationToken);
        };
        _statusPingTimer.Start();
    }

    private void SetupObsSocketEvents()
    {
        if (_obsWebSocketHandler == null)
        {
            return;
        }
        _obsWebSocketHandler.Connected += async () =>
        {
            await _channelNotificationsConnection.InvokeAsync("SendObsConnectionStatus",
                ObsConnectionStatusType.Connected, _botChannel.TwitchUserId.ToString(), _cancellationToken);
        };
        _obsWebSocketHandler.OnDisconnect += async () =>
        {
            await _channelNotificationsConnection.InvokeAsync("SendObsConnectionStatus",
                ObsConnectionStatusType.Disconnected, _botChannel.TwitchUserId.ToString(), _cancellationToken);
        };
        _obsWebSocketHandler.OnFault += async () =>
        {
            await _channelNotificationsConnection.InvokeAsync("SendObsConnectionStatus",
                ObsConnectionStatusType.Errored, _botChannel.TwitchUserId.ToString(), _cancellationToken);
        };
        
        _statusObsPingTimer.Elapsed += async (_, _) =>
        {
            var connectionStatus = _webSocketHandler.State switch
            {
                WebSocketState.Open => ObsConnectionStatusType.Connected,
                WebSocketState.Aborted => ObsConnectionStatusType.Errored,
                _ => ObsConnectionStatusType.Disconnected
            };
            await _channelNotificationsConnection.InvokeAsync("SendObsConnectionStatus",
                connectionStatus, _botChannel.TwitchUserId.ToString(), _cancellationToken);
        };
        _statusPingTimer.Start();
    }

    private async Task Authenticate()
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

    public async Task Reauthenticate(string botAccessToken)
    {
        _botAccessToken = botAccessToken;
        if (!IsConnected)
        {
            await Connect();
        }
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
                        if (_botChannel.IsHamdleEnabled && _obsWebSocketHandler != null)
                        {
                            var currentWord = _hamdleWords.ElementAt(new Random().Next(0, _hamdleWords.Count));
                            await StartHamdle(currentWord);
                        }
                        else
                        {
                            await _webSocketHandler.SendMessageToChat("Hamdle is not enabled in this channel.");
                        }

                        break;
                }

                return;
            }

            if ((ircMessage.Message?.Contains("!#guess") ?? false)
                && _hamdleContext is { IsRoundInProgress: true, IsInVotingState: false })
            {
                var guess = ircMessage.Message.Split(" ")[1];
                _hamdleContext.SubmitGuess(ircMessage.User!, guess);
                return;
            }

            var strippedCommand = ircMessage.Message!.Replace("!#", "");
            var command = _botChannel.Commands.FirstOrDefault(x =>
                string.Equals(x.Command, strippedCommand, StringComparison.CurrentCultureIgnoreCase));
            if (command != null)
            {
                await _webSocketHandler.SendMessageToChat(command.Response);
                return;
            }

            await _webSocketHandler.SendMessageToChat("Invalid command! SirSad");
        }
    }

    private async Task StartHamdle(string currentWord)
    {
        if (_hamdleContext is not null)
        {
            return;
        }

        _hamdleContext = new HamdleContext(_hamdleWords, currentWord, _botChannel.TwitchUserId,
            _hamdleHubConnection);
        await _obsWebSocketHandler!.SetHamdleSceneState(true);
        await Task.Delay(2000, _cancellationToken);
        await _webSocketHandler.SendMessageToChat(
            "Starting a new game of hamdle! To guess a word, type in !#guess your-word-here.");
        await _hamdleContext.StartGuesses();

        _hamdleContext.SendMessageToChat += async (_, hamdleMessage) =>
        {
            await _webSocketHandler.SendMessageToChat(hamdleMessage);
        };

        _hamdleContext.Restarted += async (_, _) =>
        {
            await _obsWebSocketHandler!.SetHamdleSceneState(false);
            await _webSocketHandler.SendMessageToChat("Hamdle is ending. Thanks for playing!");
            _hamdleContext = null;
        };
    }
}