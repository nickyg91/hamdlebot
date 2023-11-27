using Hamdlebot.Models.OBS;
using HamdleBot.Services.OBS;

namespace HamdleBot.Services.Mediators;

public class HamdleMediator
{
    private readonly IObsService _obsService;
    public HamdleMediator(
        IObsService obsService)
    {
        _obsService = obsService;
    }
    
    public async Task SendObsRequest<T>(ObsRequest<T> request) where T : class
    {
        await _obsService.SendRequest(request);
    }
}