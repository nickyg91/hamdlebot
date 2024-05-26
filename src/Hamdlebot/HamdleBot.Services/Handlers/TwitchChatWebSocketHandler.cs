namespace HamdleBot.Services.Handlers;

public class TwitchChatWebSocketHandler(string url, CancellationToken cancellationToken, string channelName, byte maxReconnectAttempts)
    : WebSocketHandlerBase(url, cancellationToken, maxReconnectAttempts)
{
    public async Task JoinChannel()
    {
        var ircMessage = $"JOIN #{channelName}";
        await SendMessage(ircMessage);
    }
    
    public async Task SendMessageToChat(string message)
    {
        var ircMessage = $"PRIVMSG #{channelName} :{message}";
        await SendMessage(ircMessage);
    }
}