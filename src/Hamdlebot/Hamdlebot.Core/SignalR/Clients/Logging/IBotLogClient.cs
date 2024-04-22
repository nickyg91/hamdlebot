using Hamdlebot.Core.Models.Logging;

namespace Hamdlebot.Core.SignalR.Clients.Logging;

public interface IBotLogClient : ISignalrHubClient
{
    Task LogMessage(LogMessage message);
    Task SendBotStatus(BotStatusType status);
}