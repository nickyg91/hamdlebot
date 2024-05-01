using Hamdlebot.Models.OBS;

namespace HamdleBot.Services.OBS;

public interface IObsService : IWebSocketEnabledService
{
    
    Task SendRequest<T>(ObsRequest<T> message) where T : class;
}