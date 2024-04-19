using System.Text.Json;
using System.Text.RegularExpressions;
using Hamdle.Cache;
using Hamdlebot.Core;
using Hamdlebot.Core.Models.Logging;
using Hamdlebot.Core.SignalR.Clients;
using Hamdlebot.Core.SignalR.Clients.Hamdle;
using Hamdlebot.Core.SignalR.Clients.Logging;
using Hamdlebot.Models.OBS;
using Hamdlebot.Models.OBS.RequestTypes;
using Hamdlebot.Models.OBS.ResponseTypes;
using HamdleBot.Services.Hamdle;
using HamdleBot.Services.Mediators;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HamdleBot.Services;

public partial class HamdleService : IHamdleService
{
    private Regex _onlyLetters = OnlyLetters();
    private readonly ICacheService _cache;
    private readonly IHamdleHubClient _hamdleHubClient;
    private readonly HamdleMediator _hamdleMediator;
    private readonly IBotLogClient _logClient;
    private readonly ObsSettings _obsSettings;
    private HamdleContext? _hamdleContext;
    private SceneItem? _hamdleScene;
    public event EventHandler<string>? SendMessageToChat;
    public HamdleService(
        ICacheService cache, 
        IHamdleHubClient hamdleHubClient, 
        HamdleMediator hamdleMediator,
        IOptions<AppConfigSettings> settings,
        IBotLogClient logClient)
    {
        _cache = cache;
        _hamdleHubClient = hamdleHubClient;
        _hamdleMediator = hamdleMediator;
        _logClient = logClient;
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
                    await _logClient.LogMessage(new LogMessage("Starting Hamdle Scene", DateTime.UtcNow, SeverityLevel.Info));
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

    public async Task<bool> SubmitGuess(string username, string guess)
    {
        if (!_onlyLetters.Match(guess).Success)
        {
            return false;
        }
        await _hamdleContext!.SubmitGuess(username, guess);
        return true;
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
                _hamdleScene!.SceneItemId,
                _logClient);
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

    [GeneratedRegex("^[a-zA-Z]+$")]
    private static partial Regex OnlyLetters();
}