using System.Net.WebSockets;
using Hamdlebot.Models.OBS;

namespace HamdleBot.Services.OBS;

public interface IObsService
{
    Task CreateWebSocket(CancellationToken cancellationToken);
    
    Task SendRequest<T>(ObsRequest<T> message) where T : class;
    ObsResponse<T>? ProcessMessage<T>(string message) where T : class;
    Task HandleMessages();
}