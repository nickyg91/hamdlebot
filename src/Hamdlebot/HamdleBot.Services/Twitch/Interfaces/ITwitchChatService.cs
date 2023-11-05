using System.Net.WebSockets;

namespace HamdleBot.Services.Twitch.Interfaces;

public interface ITwitchChatService
{
    Task<ClientWebSocket> CreateWebSocket(CancellationToken token);
    Task HandleMessages();
    Task WriteMessage(string message);
}