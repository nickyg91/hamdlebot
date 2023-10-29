using System.Net.WebSockets;
using Hamdlebot.Models;

namespace Hamdlebot.TwitchServices.Interfaces;

public interface ITwitchChatService
{
    Task<ClientWebSocket> CreateWebSocket(CancellationToken token);
    Task HandleMessages();
    Task WriteMessage(string message);
}