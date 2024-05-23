using System.Text.Json;
using Hamdlebot.Core.Collections;
using Hamdlebot.Core.Models.Enums;
using Hamdlebot.Core.Models.EventSub.Messages;
using Hamdlebot.Models.Twitch;
using HamdleBot.Services.Factories;
using HamdleBot.Services.Twitch.Interfaces;

namespace HamdleBot.Services.Handlers;

public class TwitchEventSubWebSocketHandler : WebSocketHandlerBase
{
    private readonly LimitedSizeHashSet<EventMessage, string> _eventSet = new(25, x => x.Metadata.MessageId);
    private readonly string _broadcasterId;
    private readonly string _userId;
    private string _sessionId = string.Empty;
    private readonly string _authToken;
    private readonly string _clientId;
    private List<SubscriptionType> _events;
    private readonly ITwitchApiService _twitchApiService;
    private readonly HashSet<SubscriptionType> _enabledSubscriptions;
    public Action<EventMessage>? OnStreamOnline { get; set; }
    public Action<EventMessage>? OnStreamOffline { get; set; }
    public Action<EventMessage>? OnChannelPollBegin { get; set; }
    public Action<EventMessage>? OnChannelPollProgress { get; set; }
    public Action<EventMessage>? OnChannelPollEnd { get; set; }
    public Action<EventMessage>? OnChannelFollow { get; set; }
    public Action<EventMessage>? OnChannelRaid { get; set; }
    public Action<EventMessage>? OnChannelSubscribe { get; set; }
    public Action<EventMessage>? OnChannelSubscriptionEnd { get; set; }
    public Action<EventMessage>? OnChannelCheer { get; set; }
    public Action<EventMessage>? OnChannelPredictionBegin { get; set; }
    public Action<EventMessage>? OnChannelPredictionLock { get; set; }
    public Action<EventMessage>? OnChannelPredictionEnd { get; set; }
    public Action<EventMessage>? OnChannelVipAdd { get; set; }
    public Action<EventMessage>? OnChannelVipRemove { get; set; }
    public Action<EventMessage>? OnChatChannelMessage { get; set; }
    public Action<EventMessage>? OnChannelBan { get; set; }
    public Action<EventMessage>? OnChannelUpdate { get; set; }
    public Action<EventMessage>? OnKeepaliveMessage { get; set; }

    public TwitchEventSubWebSocketHandler(
        string url,
        string broadcasterId,
        string userId,
        CancellationToken cancellationToken,
        byte maxReconnectAttempts,
        string authToken,
        string clientId,
        List<SubscriptionType> events) : base(url, cancellationToken, maxReconnectAttempts)
    {
        MessageReceived += OnMessageReceived;
        _broadcasterId = broadcasterId;
        _userId = userId;
        _authToken = authToken;
        _clientId = clientId;
        _events = events;
        _twitchApiService = TwitchApiServiceFactory.CreateTwitchApiService(authToken, clientId, cancellationToken);
        _enabledSubscriptions = new HashSet<SubscriptionType>();
    }

    public async Task StartEventSubscriptions()
    {
        await Task.Run(async () => await Connect());
    }

    private async void OnMessageReceived(string message)
    {
        if (message.Contains("PING"))
        {
            await HandlePong();
            return;
        }

        var eventMessage = JsonSerializer.Deserialize<EventMessage>(message);
        if (eventMessage == null)
        {
            return;
        }

        if (!_eventSet.Contains(eventMessage.Metadata.MessageId))
        {
            _eventSet.Add(eventMessage);
        }

        switch (eventMessage.Metadata.MessageType)
        {
            case MessageType.Notification:
                ProcessNotificationMessage(eventMessage);
                break;
            case MessageType.SessionWelcome:
                await ProcessSessionWelcomeMessage(eventMessage);
                break;
            case MessageType.SessionReconnect:
                break;
            case MessageType.SessionKeepalive:
                OnKeepaliveMessage?.Invoke(eventMessage);
                break;
            case MessageType.Revocation:
                break;
            default:
                //custom exception here
                throw new ArgumentOutOfRangeException("Unsupported message type.");
        }
    }

    private async Task ProcessSessionKeepaliveMessage()
    {
        await SendMessage("PING");
    }

    private async Task ProcessSessionWelcomeMessage(EventMessage eventMessage)
    {
        if (eventMessage.Payload is not null)
        {
            _sessionId = eventMessage.Payload.Session.Id;
            var subscriptionTasks =
                _events.Select(eventType => _twitchApiService.SubscribeToEvents(new EventSubRequest
                {
                    Type = eventType,
                    Transport = new EventSubTransportRequest
                    {
                        SessionId = _sessionId
                    },
                    Condition = new Dictionary<string, string>
                    {
                        ["broadcaster_user_id"] = _broadcasterId, 
                        ["user_id"] = _userId
                    },
                    Version = 1
                })).ToList();
            await Task.WhenAll(subscriptionTasks);
            var subs = subscriptionTasks.Select(x => x.Result).ToList();
            subs.Where(x => x!.Data.First().Status == "enabled").SelectMany(x => x!.Data)
                .Select(x => _enabledSubscriptions.Add(x.Type));
        }
    }

    private void ProcessNotificationMessage(EventMessage eventMessage)
    {
        switch (eventMessage.Payload?.Subscription.Type)
        {
            case SubscriptionType.StreamOnline:
                OnStreamOnline?.Invoke(eventMessage);
                break;
            case SubscriptionType.StreamOffline:
                OnStreamOffline?.Invoke(eventMessage);
                break;
            case SubscriptionType.ChannelPollBegin:
                OnChannelPollBegin?.Invoke(eventMessage);
                break;
            case SubscriptionType.ChannelPollEnd:
                OnChannelPollEnd?.Invoke(eventMessage);
                break;
            case SubscriptionType.ChannelPollProgress:
                OnChannelPollProgress?.Invoke(eventMessage);
                break;
            case SubscriptionType.ChannelRaid:
                OnChannelRaid?.Invoke(eventMessage);
                break;
            case SubscriptionType.ChannelFollow:
                OnChannelFollow?.Invoke(eventMessage);
                break;
            case SubscriptionType.ChannelChatMessage:
                OnChatChannelMessage?.Invoke(eventMessage);
                break;
            case SubscriptionType.ChannelSubscribe:
                OnChannelSubscribe?.Invoke(eventMessage);
                break;
            case SubscriptionType.ChannelSubscriptionEnd:
                OnChannelSubscriptionEnd?.Invoke(eventMessage);
                break;
            case SubscriptionType.ChannelCheer:
                OnChannelCheer?.Invoke(eventMessage);
                break;
            case SubscriptionType.ChannelBan:
                OnChannelBan?.Invoke(eventMessage);
                break;
            case SubscriptionType.ChannelUpdate:
                OnChannelUpdate?.Invoke(eventMessage);
                break;
            case SubscriptionType.ChannelPredictionBegin:
                OnChannelPredictionBegin?.Invoke(eventMessage);
                break;
            case SubscriptionType.ChannelPredictionLocked:
                OnChannelPredictionLock?.Invoke(eventMessage);
                break;
            case SubscriptionType.ChannelPredictionEnd:
                OnChannelPredictionEnd?.Invoke(eventMessage);
                break;
            case SubscriptionType.ChannelVipAdd:
                OnChannelVipAdd?.Invoke(eventMessage);
                break;
            case SubscriptionType.ChannelVipRemove:
                OnChannelVipRemove?.Invoke(eventMessage);
                break;
            case SubscriptionType.NotSupported:
                // exception here
                break;
            case null:
                // exception here
                break;
            default:
                // create new exception type here
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task HandlePong()
    {
        await SendMessage("PONG");
    }
}