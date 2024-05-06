namespace HamdleBot.Services.Handlers;

public class ObsWebSocketHandler(string url, CancellationToken cancellationToken, byte maxReconnectAttempts) : WebSocketHandlerBase(url, cancellationToken, maxReconnectAttempts)
{
}