namespace HamdleBot.Services.Handlers;

public class TwitchChatWebSocketHandler(CancellationToken cancellationToken, string channelName, byte maxReconnectAttempts)
    : WebSocketHandlerBase(cancellationToken, maxReconnectAttempts)
{
    public override string Url => "wss://irc-ws.chat.twitch.tv:443";
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

    public async Task SendPartMessage()
    {
        await SendMessage("PART");
    }
}