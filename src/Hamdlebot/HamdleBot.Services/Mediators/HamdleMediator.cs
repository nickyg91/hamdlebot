using Hamdlebot.Models.OBS;
using Hamdlebot.Models.OBS.ResponseTypes;
using HamdleBot.Services.OBS;

namespace HamdleBot.Services.Mediators;

public class HamdleMediator
{
    private readonly IObsService _obsService;
    private readonly IHamdleWordService _hamdleWordService;

    public HamdleMediator(IObsService obsService, IHamdleWordService hamdleWordService)
    {
        _obsService = obsService;
        _hamdleWordService = hamdleWordService;
    }
    
    public async Task SendObsRequest<T>(ObsRequest<T> request) where T : class
    {
        await _obsService.SendRequest(request);
    }

    public void SetHamdleSceneItem(SceneItem item)
    {
        _hamdleWordService.SetHamdleSceneItem(item);
    }
}