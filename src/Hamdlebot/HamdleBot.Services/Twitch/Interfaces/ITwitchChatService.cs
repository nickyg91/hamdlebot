using System.Net.WebSockets;

namespace HamdleBot.Services.Twitch.Interfaces;

public interface ITwitchChatService
{
    Task CreateWebSocket(CancellationToken token);
}