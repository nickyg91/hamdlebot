using System.Text.Json;
using Hamdle.Cache;
using Hamdlebot.Models.OBS;
using Hamdlebot.Models.OBS.RequestTypes;
using Hamdlebot.Models.OBS.ResponseTypes;
using HamdleBot.Services.Hamdle;
using HamdleBot.Services.Mediators;
using Microsoft.AspNetCore.SignalR.Client;
using StackExchange.Redis;

namespace HamdleBot.Services;

public class HamdleService : IHamdleService
{
    private readonly ICacheService _cache;
    private readonly HubConnection _signalRHub;
    private readonly HamdleMediator _hamdleMediator;
    private HamdleContext? _hamdleContext;
    private SceneItem? _hamdleScene;
    public event EventHandler<string>? SendMessageToChat;
    public HamdleService(
        ICacheService cache, 
        HubConnection signalRHub, 
        HamdleMediator hamdleMediator)
    {
        _cache = cache;
        _signalRHub = signalRHub;
        _hamdleMediator = hamdleMediator;
        
        _cache.Subscriber
            .Subscribe(new RedisChannel("onSceneRetrieved", RedisChannel.PatternMode.Auto)).OnMessage(async channelMessage =>
        {
            SetHamdleScene(channelMessage.Message);
            CreateHamdleContext();
            await _hamdleContext!.StartGuesses();
        });
        
        _cache.Subscriber.Subscribe(new RedisChannel("startHamdleScene", RedisChannel.PatternMode.Auto))
            .OnMessage(async channelMessage =>
            {
                if (_hamdleContext is null)
                {
                    await SendObsSceneRequest();
                }
            });
    }
    
    public bool IsHamdleSessionInProgress()
    {
        return _hamdleContext?.IsRoundInProgress ?? false;
    }

    public bool IsHamdleVotingInProgress()
    {
        return _hamdleContext?.IsInVotingState ?? false;
    }

    public async Task SubmitGuess(string username, string guess)
    {
        await _hamdleContext!.SubmitGuess(username, guess);
    }

    public void SubmitVoteForGuess(string username, int submission)
    {
        _hamdleContext!.SubmitVoteForGuess(username, submission);
    }

    private void CreateHamdleContext()
    {
        _hamdleContext = 
            new HamdleContext(
                _cache, 
                _signalRHub, 
                _hamdleMediator,
                _hamdleScene!.SceneItemId);
        _hamdleContext.SendMessageToChat += SendMessageToChat;
        _hamdleContext.Restarted += Restart_Triggered!;
    }

    private void Restart_Triggered(object sender, EventArgs e)
    {
        _hamdleContext = null;
    }
    
    private void SetHamdleScene(string? json)
    {
        if (!string.IsNullOrEmpty(json))
        {
            _hamdleScene = JsonSerializer.Deserialize<SceneItem>(json);
        }
    }

    private async Task SendObsSceneRequest()
    {
        await _hamdleMediator.SendObsRequest(new ObsRequest<GetSceneItemListRequest>
        {
            RequestData = new RequestWrapper<GetSceneItemListRequest>()
            {
                RequestId = Guid.NewGuid(),
                RequestType = ObsRequestStrings.GetSceneItemList,
                RequestData = new GetSceneItemListRequest
                {
                    // make this more flexible
                    SceneName = "Desktop Capture",   
                }
            },
            Op = OpCodeType.Request
        });
    }
}