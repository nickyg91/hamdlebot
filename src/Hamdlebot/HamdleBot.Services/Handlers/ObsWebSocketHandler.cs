namespace HamdleBot.Services.Handlers;

public class ObsWebSocketHandler(string url, CancellationToken cancellationToken) : WebSocketHandlerBase(url, cancellationToken)
{
}