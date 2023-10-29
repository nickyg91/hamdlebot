using System.Net.WebSockets;
using System.Text;
using Hamdlebot.Core;
using Hamdlebot.Models;
using Hamdlebot.TwitchServices.Interfaces;
using Microsoft.Extensions.Options;

namespace Hamdlebot.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly AppConfigSettings _settings;
    private readonly ITwitchIdentityApiService _identityApiService;
    private readonly ITwitchChatService _twitchChatService;

    public Worker(ILogger<Worker> logger, 
        IOptions<AppConfigSettings> settings, 
        ITwitchIdentityApiService identityApiService,
        ITwitchChatService twitchChatService)
    {
        _logger = logger;
        _settings = settings.Value;
        _identityApiService = identityApiService;
        _twitchChatService = twitchChatService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // var token = await _identityApiService.GetToken(new ClientCredentialsTokenRequest
        // {
        //     ClientId = _settings.TwitchConnectionInfo.ClientId,
        //     ClientSecret = _settings.TwitchConnectionInfo.ClientSecret
        // });
        await _twitchChatService.CreateWebSocket(stoppingToken);
        await _twitchChatService.HandleMessages();
        //await _twitchChatService.WriteMessage("hello");
        // while (true)
        // {
        //     //do nothing
        // }
    }
}
