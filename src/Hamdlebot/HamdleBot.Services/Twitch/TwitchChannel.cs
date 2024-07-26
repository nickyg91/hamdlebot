using System.Net.WebSockets;
using Hamdle.Cache;
using Hamdlebot.Core;
using Hamdlebot.Core.Extensions;
using Hamdlebot.Core.Models.Enums;
using Hamdlebot.Core.Models.Enums.EventSub;
using Hamdlebot.Core.Models.EventSub.Responses;
using Hamdlebot.Models;
using Hamdlebot.Models.ViewModels;
using HamdleBot.Services.Hamdle;
using HamdleBot.Services.Handlers;
using Microsoft.AspNetCore.SignalR.Client;

namespace HamdleBot.Services.Twitch;

public class TwitchChannel
{
    private Channel _botChannel;
    private readonly TwitchChatWebSocketHandler _twitchChatSocketHandler;
    private readonly TwitchEventSubWebSocketHandler _eventSubHandler;
    private ObsWebSocketHandler? _obsWebSocketHandler;
    private string _botAccessToken;
    private readonly ICacheService _cacheService;
    private readonly HubConnection _hamdleHubConnection;
    private readonly HubConnection _channelNotificationsConnection;
    private readonly CancellationToken _cancellationToken;
    private HamdleContext? _hamdleContext;
    private HashSet<string> _hamdleWords = [];
    private ObsSettings? _obsSettings;
    private readonly string _hamdlebotUserId;
    private readonly System.Timers.Timer _statusPingTimer = new(30000)
    {
        AutoReset = true
    };
    private readonly System.Timers.Timer _statusObsPingTimer = new(30000)
    {
        AutoReset = true
    };
    
    
    public bool IsConnected => _twitchChatSocketHandler.State == WebSocketState.Open
                               && _twitchChatSocketHandler.State != WebSocketState.Connecting;

    private readonly List<string> _baseChannelCommands =
    [
        "!#commands",
        "!#random",
        "!#hamdle",
        "!#guess"
    ];

    public TwitchChannel(
        TwitchChannelAggregate twitchChannelAggregate,
        ICacheService cacheService,
        CancellationToken cancellationToken)
    {
        _botAccessToken = twitchChannelAggregate.BotAccessToken;
        _cacheService = cacheService;
        _hamdleHubConnection = twitchChannelAggregate.HamdleHubConnection;
        _channelNotificationsConnection = twitchChannelAggregate.ChannelNotificationsConnection;
        _botChannel = twitchChannelAggregate.Channel;
        _twitchChatSocketHandler =
            new TwitchChatWebSocketHandler
            (
                cancellationToken,
                _botChannel.TwitchChannelName,
                3
            );
        SetupTwitchIrcSocketEvents();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);
        _cancellationToken = cts.Token;
        _obsSettings = twitchChannelAggregate.ObsSettings;
        _hamdlebotUserId = twitchChannelAggregate.HamdlebotUserId;
        
        _eventSubHandler = new TwitchEventSubWebSocketHandler(
            _botChannel.TwitchUserId.ToString(),
            _hamdlebotUserId,
            cancellationToken,
            3,
            _botAccessToken,
            twitchChannelAggregate.ClientId,
            [SubscriptionType.StreamOnline, SubscriptionType.StreamOffline]
        );
        SetupEventSubEvents();
    }

    public async Task LeaveChannel()
    {
        await _twitchChatSocketHandler.SendMessageToChat("I'm leaving the channel. Bye!");
        await _twitchChatSocketHandler.SendPartMessage();
        await _twitchChatSocketHandler.Disconnect();
    }

    public async Task Connect()
    {
        if (_twitchChatSocketHandler.State != WebSocketState.Open
            && _twitchChatSocketHandler.State != WebSocketState.Connecting)
        {
            await Task.Run(async () => await _twitchChatSocketHandler.Connect(), _cancellationToken);
            await Task.Run(async () => await _eventSubHandler.StartEventSubscriptions(), _cancellationToken);
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
        _twitchChatSocketHandler.MessageReceived += async message => { await OnMessageReceived(message); };
        _twitchChatSocketHandler.Connected += async () =>
        {
            await Authenticate();
            await _twitchChatSocketHandler.SendMessageToChat("Never fear, hamdlebot is here!");
            await SendTwitchConnectionPing(ChannelConnectionStatusType.Connected);
        };
        _twitchChatSocketHandler.OnDisconnect += async () =>
        {
            await SendTwitchConnectionPing(ChannelConnectionStatusType.Disconnected);
        };
        _twitchChatSocketHandler.OnFault += async () =>
        {
            await SendTwitchConnectionPing(ChannelConnectionStatusType.Errored);
        };
        
        _statusPingTimer.Elapsed += async (_, _) =>
        {
            var connectionStatus = _twitchChatSocketHandler.State switch
            {
                WebSocketState.Open => ChannelConnectionStatusType.Connected,
                WebSocketState.Aborted => ChannelConnectionStatusType.Errored,
                _ => ChannelConnectionStatusType.Disconnected
            };
            await SendTwitchConnectionPing(connectionStatus);
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
            await SendObsConnectionPing(ObsConnectionStatusType.Connected);
        };
        _obsWebSocketHandler.OnDisconnect += async () =>
        {
            await SendObsConnectionPing(ObsConnectionStatusType.Disconnected);
        };
        _obsWebSocketHandler.OnFault += async () =>
        {
            await SendObsConnectionPing(ObsConnectionStatusType.Errored);
        };
        
        _statusObsPingTimer.Elapsed += async (_, _) =>
        {
            var connectionStatus = _twitchChatSocketHandler.State switch
            {
                WebSocketState.Open => ObsConnectionStatusType.Connected,
                WebSocketState.Aborted => ObsConnectionStatusType.Errored,
                _ => ObsConnectionStatusType.Disconnected
            };
            await SendObsConnectionPing(connectionStatus);
        };
        _statusPingTimer.Start();
    }

    private void SetupEventSubEvents()
    {
        _eventSubHandler.OnStreamOnline += HandleStreamOnline;
        _eventSubHandler.OnStreamOffline += HandleStreamOffline;
    }
    
    private async Task Authenticate()
    {
        var capReq = $"CAP REQ :twitch.tv/membership twitch.tv/tags twitch.tv/commands";
        var pass = $"PASS oauth:{_botAccessToken}";
        var nick = "NICK hamdlebot";
        await _twitchChatSocketHandler.SendMessage(pass);
        await _twitchChatSocketHandler.SendMessage(nick);
        await _twitchChatSocketHandler.SendMessage(capReq);
        await Task.Delay(3000);
        await _twitchChatSocketHandler.JoinChannel();
    }

    public async Task Reauthenticate(string botAccessToken)
    {
        _botAccessToken = botAccessToken;
        await _eventSubHandler.Disconnect();
        _eventSubHandler.SetNewAuthToken(_botAccessToken);
        if (!IsConnected)
        {
            await Connect();
        }
    }

    private async Task OnMessageReceived(string message)
    {
        if (message.IsPingMessage())
        {
            await _twitchChatSocketHandler.SendMessage("PONG :tmi.twitch.tv");
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
                        await _twitchChatSocketHandler.SendMessageToChat("Commands: !#commands, !#random, !#hamdle");
                        break;
                    case "!#hamdle":
                        if (_botChannel.IsHamdleEnabled && _obsWebSocketHandler != null)
                        {
                            var currentWord = _hamdleWords.ElementAt(new Random().Next(0, _hamdleWords.Count));
                            await StartHamdle(currentWord);
                        }
                        else
                        {
                            await _twitchChatSocketHandler.SendMessageToChat("Hamdle is not enabled in this channel.");
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
                await _twitchChatSocketHandler.SendMessageToChat(command.Response);
                return;
            }

            await _twitchChatSocketHandler.SendMessageToChat("Invalid command! SirSad");
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
        await _twitchChatSocketHandler.SendMessageToChat(
            "Starting a new game of hamdle! To guess a word, type in !#guess your-word-here.");
        await _hamdleContext.StartGuesses();

        _hamdleContext.SendMessageToChat += async (_, hamdleMessage) =>
        {
            await _twitchChatSocketHandler.SendMessageToChat(hamdleMessage);
        };

        _hamdleContext.Restarted += async (_, _) =>
        {
            await _obsWebSocketHandler!.SetHamdleSceneState(false);
            await _twitchChatSocketHandler.SendMessageToChat("Hamdle is ending. Thanks for playing!");
            _hamdleContext = null;
        };
    }

    private async Task SendTwitchConnectionPing(ChannelConnectionStatusType status)
    {
        await _channelNotificationsConnection.InvokeAsync("SendChannelConnectionStatus",
            status, _botChannel.TwitchUserId.ToString(), _cancellationToken);
    }

    private async Task SendObsConnectionPing(ObsConnectionStatusType status)
    {
        await _channelNotificationsConnection.InvokeAsync("SendObsConnectionStatus",
            status, _botChannel.TwitchUserId.ToString(), _cancellationToken);
    }

    private void HandleStreamOnline(StreamOnlineEvent evt)
    {
        Task.Run(async () =>
        {
            await Connect();
            if (_obsSettings != null)
            {
                await ConnectToObs();
            }
        }, _cancellationToken);
    }

    private void HandleStreamOffline(StreamOfflineEvent evt)
    {
        Task.Run(async () =>
        {
            await _twitchChatSocketHandler.SendMessageToChat("Stream is offline. See you later!");
            await _twitchChatSocketHandler.Disconnect();
            if (_obsSettings != null)
            {
                await DisconnectFromObs();
            }
        }, _cancellationToken);
    }
}