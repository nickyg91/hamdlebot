using System.Text.Json;
using Hamdlebot.Core.Collections;
using Hamdlebot.Core.Exceptions;
using Hamdlebot.Core.Models.Enums;
using Hamdlebot.Core.Models.EventSub.Messages;
using Hamdlebot.Models.Twitch;
using HamdleBot.Services.Factories;
using HamdleBot.Services.Twitch.Interfaces;
using Timer = System.Timers.Timer;

namespace HamdleBot.Services.Handlers;

public class TwitchEventSubWebSocketHandler : WebSocketHandlerBase
{
    private readonly LimitedSizeHashSet<EventMessage, string> _eventSet = new(25, x => x.Metadata.MessageId);
    private readonly string _broadcasterId;
    private readonly string _userId;
    private readonly string _clientId;
    private string _authToken;
    private ITwitchApiService _twitchApiService;
    private string _sessionId = string.Empty;
    private int _keepaliveTimeoutSeconds;
    private List<SubscriptionType> _events;
    private Timer _keepaliveTimer;
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
    public Action<EventMessage>? OnWelcomeMessage { get; set; }
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
        _clientId = clientId;
        _authToken = authToken;
        _broadcasterId = broadcasterId;
        _userId = userId;
        _events = events;
        _twitchApiService = TwitchApiServiceFactory.CreateTwitchApiService(_authToken, _clientId, CancellationToken);
    }

    public async Task StartEventSubscriptions()
    {
        await Task.Run(async () => await Connect());
    }

    public void SetNewAuthToken(string authToken)
    {
        _authToken = authToken;
        _twitchApiService = TwitchApiServiceFactory.CreateTwitchApiService(_authToken, _clientId, CancellationToken);
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
                throw new InvalidMetadataMessageType("Unsupported message type.");
        }
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
            _keepaliveTimeoutSeconds = eventMessage.Payload.Session.KeepaliveTimeoutSeconds;
            StartKeepaliveTimer();
            OnWelcomeMessage?.Invoke(eventMessage);
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
            case null:
            case SubscriptionType.NotSupported:
                throw new SubscriptionEventNotSupportedException("Subscription event not supported.");
            default:
                // create new exception type here
                throw new InvalidSubscriptionTypeException($"Invalid subscription type: {eventMessage.Payload?.Subscription.Type}");
        }
    }

    private async Task HandlePong()
    {
        await SendMessage("PONG");
    }

    private void StartKeepaliveTimer()
    {
        _keepaliveTimer = new Timer(TimeSpan.FromSeconds(_keepaliveTimeoutSeconds));
        _keepaliveTimer.Elapsed += async (_, _) =>
        {
            var rightNow = DateTime.UtcNow;
            var lastEvent = _eventSet.LastItem();
            if (rightNow.Subtract(lastEvent.Metadata.MessageTimestamp).Seconds < _keepaliveTimeoutSeconds - 3)
            {
                return;
            }
            await Disconnect();
            await StartEventSubscriptions();
        };
        _keepaliveTimer.Start();
    }
}