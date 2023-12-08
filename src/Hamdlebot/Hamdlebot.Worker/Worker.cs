using Hamdle.Cache;
using Hamdlebot.Models.OBS;
using Hamdlebot.Models.OBS.RequestTypes;
using HamdleBot.Services;
using HamdleBot.Services.OBS;
using HamdleBot.Services.Twitch.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;

namespace Hamdlebot.Worker;

public class Worker : BackgroundService
{
    private readonly ITwitchChatService _twitchChatService;
    private readonly IWordService _wordService;
    private readonly IObsService _obsService;
    private readonly HubConnection _signalr;

    public Worker(
        ITwitchChatService twitchChatService,
        IWordService wordService,
        HubConnection signalr,
        IObsService obsService)
    {
        _twitchChatService = twitchChatService;
        _wordService = wordService;
        _signalr = signalr;
        _obsService = obsService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.WhenAll(
            _signalr.StartAsync(stoppingToken),
            _obsService.CreateWebSocket(stoppingToken),
            _wordService.InsertWords(),
            _twitchChatService.CreateWebSocket(stoppingToken));

        Task.Run(() => _twitchChatService.HandleMessages(), stoppingToken);
        Task.Run(() => _obsService.HandleMessages(), stoppingToken);
    }
}