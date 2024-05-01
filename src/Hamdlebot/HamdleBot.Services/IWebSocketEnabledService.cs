namespace HamdleBot.Services;

public interface IWebSocketEnabledService
{
    Task CreateWebSocket(CancellationToken cancellationToken);
}