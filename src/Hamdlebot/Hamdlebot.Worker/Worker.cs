using Hamdle.Cache;
using HamdleBot.Services;
using HamdleBot.Services.Twitch.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;

namespace Hamdlebot.Worker;

public class Worker : BackgroundService
{
    private readonly ITwitchChatService _twitchChatService;
    private readonly IHamdleWordService _wordService;
    private readonly HubConnection _signalr;

    public Worker(
        ITwitchChatService twitchChatService,
        IHamdleWordService wordService,
        HubConnection signalr)
    {
        _twitchChatService = twitchChatService;
        _wordService = wordService;
        _signalr = signalr;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _signalr.StartAsync(stoppingToken);
        await _wordService.InsertValidCommands();
        await _wordService.InsertWords();
        await _twitchChatService.CreateWebSocket(stoppingToken);
        await _twitchChatService.HandleMessages();
    }
}
