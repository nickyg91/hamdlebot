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

    public HamdlebotWorker(
        ITwitchChatService twitchChatService,
        IWordService wordService,
        [FromKeyedServices("hamdleHub")] HubConnection hamdleHub,
        [FromKeyedServices("logHub")] HubConnection logHub,
        IObsService obsService,
        IHostApplicationLifetime appLifetime,
        IBotLogClient logClient)
    {
        _twitchChatService = twitchChatService;
        _wordService = wordService;
        _hamdleHub = hamdleHub;
        _logHub = logHub;
        _obsService = obsService;
        _appLifetime = appLifetime;
        _logClient = logClient;
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
        var twitchChatSocket = _twitchChatService.CreateWebSocket(cancellationToken);
        await Task.WhenAll(
            _obsService.CreateWebSocket(cancellationToken),
            _wordService.InsertWords(),
            twitchChatSocket);
        
        
        
        Task.Run(() => _twitchChatService.HandleMessages());
        Task.Run(() => _obsService.HandleMessages());
        var timer = new System.Timers.Timer();
        timer.Interval = 60000;
        timer.Elapsed += BotPong;
        timer.Start();

    }

    private void BotPong(object sender, System.Timers.ElapsedEventArgs e)
    {
        _logClient.LogMessage(new LogMessage("PONG.", DateTime.UtcNow, SeverityLevel.Info));
        _logClient.SendBotStatus(BotStatusType.Online);
    }
}
