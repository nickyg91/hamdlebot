using System.Text.RegularExpressions;
using Hamdle.Cache;
using Hamdle.Cache.Channels;
using Hamdlebot.Core;
using Hamdlebot.Core.Models.Logging;
using Hamdlebot.Core.SignalR.Clients.Hamdle;
using Hamdlebot.Core.SignalR.Clients.Logging;
using Hamdlebot.Models.OBS;
using Hamdlebot.Models.OBS.RequestTypes;
using Hamdlebot.Models.OBS.ResponseTypes;
using HamdleBot.Services.Factories;
using HamdleBot.Services.Hamdle;
using HamdleBot.Services.OBS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HamdleBot.Services;

public partial class HamdleService : IHamdleService, IProcessCacheMessage, IDisposable
{
    private Regex _onlyLetters = OnlyLetters();
    private readonly ICacheService _cache;
    private readonly IHamdleHubClient _hamdleHubClient;
    private readonly IBotLogClient _logClient;
    private readonly IObsService _obsService;
    private readonly ILogger<HamdleService> _logger;
    private ObsSettings _obsSettings;
    private HamdleContext? _hamdleContext;
    private SceneItem? _hamdleScene;
    private readonly CancellationTokenSource _cancellationToken;
    private readonly ChannelSubscription<ObsSettings> _obsSettingsStream;
    private readonly ChannelSubscription<SceneItem> _sceneRetrievedStream;
    private readonly ChannelSubscription<string> _startHamdleSceneStream;
    public event EventHandler<string>? SendMessageToChat;
    public HamdleService(
        ICacheService cache, 
        IHamdleHubClient hamdleHubClient, 
        IOptions<AppConfigSettings> settings,
        IBotLogClient logClient,
        IObsService obsService,
        ILogger<HamdleService> logger)
    {
        _cache = cache;
        _hamdleHubClient = hamdleHubClient;
        _logClient = logClient;
        _obsService = obsService;
        _logger = logger;
        _obsSettings = settings.Value.ObsSettingsOptions!;
        var sceneRetreivedChannel = new RedisChannel(RedisChannelType.OnSceneReceived, RedisChannel.PatternMode.Auto);
        var startHamdleSceneChannel = new RedisChannel(RedisChannelType.StartHamdleScene, RedisChannel.PatternMode.Auto);
        var obsSettingsChannel = new RedisChannel(RedisChannelType.ObsSettingsChanged, RedisChannel.PatternMode.Auto);
        _cancellationToken = new CancellationTokenSource();
        
        _obsSettingsStream = ChannelSubscriptionFactory.CreateSubscription<ObsSettings>(_cache, obsSettingsChannel);
        _sceneRetrievedStream = ChannelSubscriptionFactory.CreateSubscription<SceneItem>(_cache, sceneRetreivedChannel);
        _startHamdleSceneStream = ChannelSubscriptionFactory.CreateSubscription<string>(_cache, startHamdleSceneChannel);
        Task.Run(SetupStreamSubscriptions);
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
                _hamdleScene!.SceneItemId,
                _logClient, 
                _obsSettings,
                _obsService,
                _logger);
        _logClient.SendBotStatus(BotStatusType.HamdleInProgress);
        _hamdleContext.SendMessageToChat += SendMessageToChat;
        _hamdleContext.Restarted += Restart_Triggered!;
    }

    private void Restart_Triggered(object sender, EventArgs e)
    {
        _hamdleContext = null;
        _logClient.SendBotStatus(BotStatusType.Online);
    }
    
    private void SetHamdleScene(SceneItem? sceneItem)
    {
        _hamdleScene = sceneItem;
    }

    private async Task SendObsSceneRequest()
    {
        await _obsService.SendRequest(new ObsRequest<GetSceneItemListRequest>
        {
            RequestData = new RequestWrapper<GetSceneItemListRequest>()
            {
                RequestId = Guid.NewGuid(),
                RequestType = ObsRequestStrings.GetSceneItemList,
                RequestData = new GetSceneItemListRequest
                {
                    SceneName = _obsSettings.SceneName!,
                }
            },
            Op = OpCodeType.Request
        });
    }

    public async Task SetupStreamSubscriptions()
    {
        
        var hamdleSceneStreamTask = Task.Run(async () =>
        {
            await foreach (var item in _startHamdleSceneStream.Subscribe(_cancellationToken.Token))
            {
                if (_hamdleContext is not null)
                {
                    continue;
                }
                _logger.LogInformation("Starting Hamdle Scene");
                await _logClient.LogMessage(new LogMessage("Starting Hamdle Scene", DateTime.UtcNow, SeverityLevel.Info));
                await SendObsSceneRequest();
            }
        });

        var obsSettingsStreamTask = Task.Run(async () =>
        {
            await foreach (var item in _obsSettingsStream.Subscribe(_cancellationToken.Token))
            {
                _obsSettings = item;
            }
        });

        var sceneRetrievedStreamTask = Task.Run(async () =>
        {
            await foreach (var item in _sceneRetrievedStream.Subscribe(_cancellationToken.Token))
            {
                if (_hamdleContext is not null)
                {
                    continue;
                }

                SetHamdleScene(item);
                CreateHamdleContext();
                await _hamdleContext!.StartGuesses();
            }
        });

        await Task.WhenAll(sceneRetrievedStreamTask, obsSettingsStreamTask, hamdleSceneStreamTask);
    }

    [GeneratedRegex("^[a-zA-Z]+$")]
    private static partial Regex OnlyLetters();

    public void Dispose()
    {
        _cancellationToken.Cancel();
        _cancellationToken.Dispose();
    }
}