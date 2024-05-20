using System.Text.Json;
using Hamdlebot.Core.Collections;
using Hamdlebot.Core.Models.Enums;
using Hamdlebot.Core.Models.EventSub.Messages;

namespace HamdleBot.Services.Handlers;

public class TwitchEventSubWebSocketHandler : WebSocketHandlerBase
{
    private readonly LimitedSizeHashSet<EventMessage, string> _eventSet = new(25, x => x.Metadata.MessageId);
    private readonly long _broadcasterId;
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
    public Action<EventMessage>? OnChannelPredictionProgress { get; set; }
    public Action<EventMessage>? OnChannelPredictionLock { get; set; }
    
    public TwitchEventSubWebSocketHandler(string url, long broadcasterId, CancellationToken cancellationToken, byte maxReconnectAttempts) : base(url, cancellationToken, maxReconnectAttempts)
    {
        MessageReceived += OnMessageReceived;
        _broadcasterId = broadcasterId;
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
        }
    }

    private async Task ProcessSessionWelcomeMessage(EventMessage eventMessage)
    {
        await SendMessage("");
    }
    
    private void ProcessNotificationMessage(EventMessage eventMessage)
    {
        
    }

    private async Task HandlePong()
    {
        await SendMessage("PONG");
    }
}