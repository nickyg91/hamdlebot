using Hamdle.Cache;
using HamdleBot.Services;
using HamdleBot.Services.Twitch.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;

namespace Hamdlebot.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ITwitchChatService _twitchChatService;
    private readonly ICacheService _cache;
    private readonly IHamdleWordService _wordService;
    private readonly HubConnection _signalr;

    public Worker(ILogger<Worker> logger, 
        ITwitchChatService twitchChatService,
        ICacheService cache,
        IHamdleWordService wordService,
        HubConnection signalr)
    {
        _logger = logger;
        _twitchChatService = twitchChatService;
        _cache = cache;
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
