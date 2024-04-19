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
    private readonly HubConnection _signalr;
    private readonly IHostApplicationLifetime _appLifetime;
    public HamdlebotWorker(
        ITwitchChatService twitchChatService,
        IWordService wordService,
        HubConnection signalr,
        IObsService obsService,
        IHostApplicationLifetime appLifetime)
    {
        _twitchChatService = twitchChatService;
        _wordService = wordService;
        _signalr = signalr;
        _obsService = obsService;
        _appLifetime = appLifetime;
    }
    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _appLifetime.ApplicationStarted.Register(Start);
        return Task.CompletedTask;

        async void Start() => await OnStarted(cancellationToken);
    }
    
    private async Task OnStarted(CancellationToken cancellationToken)
    {
        // The application has fully started, start the background tasks
        await Task.WhenAll(
            _signalr.StartAsync(cancellationToken),
            _obsService.CreateWebSocket(cancellationToken),
            _wordService.InsertWords(),
            _twitchChatService.CreateWebSocket(cancellationToken));

        Task.Run(() => _twitchChatService.HandleMessages());
        Task.Run(() => _obsService.HandleMessages());
    }
}