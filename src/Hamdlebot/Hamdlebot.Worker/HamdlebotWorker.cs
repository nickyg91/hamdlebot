using Hamdle.Cache;
using Hamdlebot.Core.Models.Logging;
using Hamdlebot.Core.SignalR.Clients.Logging;
using HamdleBot.Services;
using HamdleBot.Services.OBS;
using HamdleBot.Services.Twitch.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;

namespace Hamdlebot.Worker;

public class HamdlebotWorker : BackgroundService
{
    private readonly ITwitchChatService _twitchChatService;
    private readonly IWordService _wordService;
    private readonly IObsService _obsService;
    private readonly HubConnection _hamdleHub;
    private readonly HubConnection _logHub;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IBotLogClient _logClient;
    private readonly ICacheService _cacheService;
    private readonly ITwitchIdentityApiService _identityApiService;
    public HamdlebotWorker(
        ITwitchChatService twitchChatService,
        IWordService wordService,
        [FromKeyedServices("hamdleHub")] HubConnection hamdleHub,
        [FromKeyedServices("logHub")] HubConnection logHub,
        IObsService obsService,
        IHostApplicationLifetime appLifetime,
        IBotLogClient logClient,
        ICacheService cacheService,
        ITwitchIdentityApiService identityApiService)
    {
        _twitchChatService = twitchChatService;
        _wordService = wordService;
        _hamdleHub = hamdleHub;
        _logHub = logHub;
        _obsService = obsService;
        _appLifetime = appLifetime;
        _logClient = logClient;
        _cacheService = cacheService;
        _identityApiService = identityApiService;
    }
    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _appLifetime.ApplicationStarted.Register(Start);
        return Task.CompletedTask;

        async void Start() => await OnStarted(cancellationToken);
    }
    
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logClient.SendBotStatus(BotStatusType.Offline);
        return base.StopAsync(cancellationToken);
    }
    
    private async Task OnStarted(CancellationToken cancellationToken)
    {
        // The application has fully started, start the background tasks
        await Task.WhenAll(
            _hamdleHub.StartAsync(cancellationToken),
            _logHub.StartAsync(cancellationToken)
        );
        await _logClient.LogMessage(new LogMessage("Bot connected.", DateTime.UtcNow, SeverityLevel.Info));
        await _logClient.SendBotStatus(BotStatusType.Online);

        await _wordService.InsertWords();
        try
        {
            await _obsService.CreateWebSocket(cancellationToken);
        }
        catch (Exception e)
        {
            await _logClient.LogMessage(new LogMessage("Connection to OBS failed.", DateTime.UtcNow, SeverityLevel.Error));
        }
        await _twitchChatService.CreateWebSocket(cancellationToken);
        _ = Task.Run(async () =>
        {
            var ms = TimeSpan.FromHours(3.5);
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var token = await _cacheService.GetItem(CacheKeyType.TwitchRefreshToken);
                try
                {
                    if (token == null)
                    {
                        return;
                    }
                    var newToken = await _identityApiService.RefreshToken(token);
                    await _cacheService.AddItem(CacheKeyType.TwitchRefreshToken, newToken.RefreshToken);
                    await _cacheService.AddItem(CacheKeyType.TwitchAccessToken, newToken.AccessToken);
                }
                catch (Exception e)
                {
                    await _logClient.LogMessage(new LogMessage($"Unable to refresh token: {e.Message}.", DateTime.UtcNow, SeverityLevel.Error));
                }
                
                await Task.Delay(ms, cancellationToken);
            }
        }, cancellationToken);
        var timer = new System.Timers.Timer();
        timer.Interval = 60000;
        timer.Elapsed += BotPong!;
        timer.Start();
    }

    private void BotPong(object sender, System.Timers.ElapsedEventArgs e)
    {
        _logClient.LogMessage(new LogMessage("PONG.", DateTime.UtcNow, SeverityLevel.Info));
        _logClient.SendBotStatus(BotStatusType.Online);
    }
}
