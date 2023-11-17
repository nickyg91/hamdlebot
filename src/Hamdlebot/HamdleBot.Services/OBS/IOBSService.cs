using System.Net.WebSockets;
using Hamdlebot.Models.OBS;

namespace HamdleBot.Services.OBS;

public interface IOBSService
{
    Task CreateWebSocket(CancellationToken cancellationToken);
    
    Task SendRequest<T>(OBSRequest<T> message) where T : class;
    Task<OBSResponse<T>?> ProcessMessage<T>(string message) where T : class;
    Task HandleMessages();
}