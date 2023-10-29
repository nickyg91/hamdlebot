using Hamdle.Cache;
using HamdleBot.Services;
using Hamdlebot.TwitchServices.Interfaces;

namespace Hamdlebot.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ITwitchChatService _twitchChatService;
    private readonly ICacheService _cache;
    private readonly IHamdleWordService _wordService;

    public Worker(ILogger<Worker> logger, 
        ITwitchChatService twitchChatService,
        ICacheService cache,
        IHamdleWordService wordService)
    {
        _logger = logger;
        _twitchChatService = twitchChatService;
        _cache = cache;
        _wordService = wordService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _wordService.InsertValidCommands();
        await _wordService.InsertWords();
        await _twitchChatService.CreateWebSocket(stoppingToken);
        await _twitchChatService.HandleMessages();
    }
}
