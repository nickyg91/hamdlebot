using System.Text.Json;
using Hamdlebot.Core.Collections;
using Hamdlebot.Core.Exceptions;
using Hamdlebot.Core.Models.Enums;
using Hamdlebot.Core.Models.Enums.EventSub;
using Hamdlebot.Core.Models.EventSub;
using Hamdlebot.Core.Models.EventSub.Messages;
using Hamdlebot.Core.Models.EventSub.Responses;
using Hamdlebot.Models.Twitch;
using HamdleBot.Services.Factories;
using HamdleBot.Services.Twitch.Interfaces;
using Timer = System.Timers.Timer;

namespace HamdleBot.Services.Handlers;

public class TwitchEventSubWebSocketHandler : WebSocketHandlerBase
{
    private readonly LimitedSizeHashSet<EventMessage<PayloadBase>, string> _eventSet = new(25, x => x.Metadata.MessageId);
    private readonly string _broadcasterId;
    private readonly string _userId;
    private readonly string _clientId;
    private string _authToken;
    private ITwitchApiService _twitchApiService;
    private int _keepaliveTimeoutSeconds;
    private List<SubscriptionType> _events;
    private Timer _keepaliveTimer;
    //public override string Url => "ws://localhost:8080/ws";
    public override string Url => "wss://eventsub.wss.twitch.tv/ws?keepalive_timeout_seconds=60";
    public Action<StreamOnlineEvent>? OnStreamOnline { get; set; }
    public Action<StreamOfflineEvent>? OnStreamOffline { get; set; }
    // public Action<EventMessage>? OnChannelPollBegin { get; set; }
    // public Action<EventMessage>? OnChannelPollProgress { get; set; }
    // public Action<EventMessage>? OnChannelPollEnd { get; set; }
    // public Action<EventMessage>? OnChannelFollow { get; set; }
    // public Action<EventMessage>? OnChannelRaid { get; set; }
    // public Action<EventMessage>? OnChannelSubscribe { get; set; }
    // public Action<EventMessage>? OnChannelSubscriptionEnd { get; set; }
    // public Action<EventMessage>? OnChannelCheer { get; set; }
    // public Action<EventMessage>? OnChannelPredictionBegin { get; set; }
    // public Action<EventMessage>? OnChannelPredictionLock { get; set; }
    // public Action<EventMessage>? OnChannelPredictionEnd { get; set; }
    // public Action<EventMessage>? OnChannelVipAdd { get; set; }
    // public Action<EventMessage>? OnChannelVipRemove { get; set; }
    // public Action<EventMessage>? OnChatChannelMessage { get; set; }
    // public Action<EventMessage>? OnChannelBan { get; set; }
    // public Action<EventMessage>? OnChannelUpdate { get; set; }
    public Action<EventMessage<PayloadBase>>? OnKeepaliveMessage { get; set; }
    public Action<Session>? OnWelcomeMessage { get; set; }
    
    //TODO at some point we need to implement a way to unsubscribe/subscribe to events dynamically
    public TwitchEventSubWebSocketHandler(
        string broadcasterId,
        string userId,
        CancellationToken cancellationToken,
        byte maxReconnectAttempts,
        string authToken,
        string clientId,
        List<SubscriptionType> events) : base(cancellationToken, maxReconnectAttempts)
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

        var baseEvent = JsonSerializer.Deserialize<EventMessage<PayloadBase>>(message);
        if (baseEvent == null)
        {
            return;
        }

        if (!_eventSet.Contains(baseEvent.Metadata.MessageId))
        {
            _eventSet.Add(baseEvent);
        }
        switch (baseEvent.Metadata.MessageType)
        {
            case MessageType.Notification:
                ProcessNotificationMessage(message);
                break;
            case MessageType.SessionWelcome:
                await ProcessSessionWelcomeMessage(baseEvent.Payload!.Session);
                break;
            case MessageType.SessionReconnect:
                break;
            case MessageType.SessionKeepalive:
                OnKeepaliveMessage?.Invoke(baseEvent);
                break;
            case MessageType.Revocation:
                break;
            default:
                throw new InvalidMetadataMessageTypeException("Unsupported message type.");
        }
    }
    
    private async Task ProcessSessionWelcomeMessage(Session? session)
    {
        try
        {
            if (session is not null)
            {
                var subscriptionTasks =
                    _events.Select(eventType => _twitchApiService.SubscribeToEvents(new EventSubRequest
                    {
                        Type = eventType,
                        Transport = new EventSubTransportRequest
                        {
                            SessionId = session.Id
                        },
                        Condition = new Dictionary<string, string>
                        {
                            ["broadcaster_user_id"] = _broadcasterId, 
                        },
                        Version = "1"
                    })).ToList();
                await Task.WhenAll(subscriptionTasks);
                _keepaliveTimeoutSeconds = session.KeepaliveTimeoutSeconds;
                //StartKeepaliveTimer();
                OnWelcomeMessage?.Invoke(session);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void ProcessNotificationMessage(string eventJson)
    {
        var baseMessage = JsonSerializer.Deserialize<EventMessage<PayloadBase>>(eventJson);

        if (baseMessage?.Payload?.Subscription == null)
        {
            return;
        }
        
        switch (baseMessage.Payload.Subscription.Type)
        {
            case SubscriptionType.StreamOnline:
                var streamOnlineMessage = DeserializeEvent<EventMessage<StreamOnlineEvent>>(eventJson);
                if (streamOnlineMessage?.Payload?.Event != null)
                {
                    OnStreamOnline?.Invoke(streamOnlineMessage.Payload.Event);    
                }
                break;
            case SubscriptionType.StreamOffline:
                var streamOfflineMessage = DeserializeEvent<EventMessage<StreamOfflineEvent>>(eventJson);
                if (streamOfflineMessage?.Payload?.Event != null)
                {   
                    OnStreamOffline?.Invoke(streamOfflineMessage.Payload.Event);
                }
                break;
            case SubscriptionType.NotSupported:
                throw new SubscriptionEventNotSupportedException("Subscription event not supported.");
            default:
                // create new exception type here
                throw new InvalidSubscriptionTypeException($"Invalid subscription type: {baseMessage.Payload?.Subscription.Type}");
        }
    }

    private static T? DeserializeEvent<T>(string eventJson)
    {
        return JsonSerializer.Deserialize<T>(eventJson);
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
            //await Disconnect();
            //await StartEventSubscriptions();
        };
        _keepaliveTimer.Start();
    }
}