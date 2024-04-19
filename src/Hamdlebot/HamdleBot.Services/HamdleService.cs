using System.Text.Json;
using Hamdle.Cache;
using Hamdlebot.Core;
using Hamdlebot.Core.SignalR.Clients;
using Hamdlebot.Models.OBS;
using Hamdlebot.Models.OBS.RequestTypes;
using Hamdlebot.Models.OBS.ResponseTypes;
using HamdleBot.Services.Hamdle;
using HamdleBot.Services.Mediators;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HamdleBot.Services;

public class HamdleService : IHamdleService
{
    private readonly ICacheService _cache;
    private readonly IHamdleHubClient _hamdleHubClient;
    private readonly HamdleMediator _hamdleMediator;
    private readonly ObsSettings _obsSettings;
    private HamdleContext? _hamdleContext;
    private SceneItem? _hamdleScene;
    public event EventHandler<string>? SendMessageToChat;
    public HamdleService(
        ICacheService cache, 
        IHamdleHubClient hamdleHubClient, 
        HamdleMediator hamdleMediator,
        IOptions<AppConfigSettings> settings)
    {
        _cache = cache;
        _hamdleHubClient = hamdleHubClient;
        _hamdleMediator = hamdleMediator;
        _obsSettings = settings.Value.ObsSettingsOptions;

        _cache.Subscriber
            .Subscribe(new RedisChannel("onSceneRetrieved", RedisChannel.PatternMode.Auto)).OnMessage(async channelMessage =>
        {
            SetHamdleScene(channelMessage.Message);
            CreateHamdleContext();
            await _hamdleContext!.StartGuesses();
        });
        
        _cache.Subscriber.Subscribe(new RedisChannel("startHamdleScene", RedisChannel.PatternMode.Auto))
            .OnMessage(async _ =>
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
                _hamdleHubClient, 
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
                    SceneName = _obsSettings.SceneName,   
                }
            },
            Op = OpCodeType.Request
        });
    }
}