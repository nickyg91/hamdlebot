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
    private readonly IHamdleWordService _wordService;
    private readonly IOBSService _obsService;
    private readonly HubConnection _signalr;

    public Worker(
        ITwitchChatService twitchChatService,
        IHamdleWordService wordService,
        HubConnection signalr,
        IOBSService obsService)
    {
        _twitchChatService = twitchChatService;
        _wordService = wordService;
        _signalr = signalr;
        _obsService = obsService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //await _obsService.CreateWebSocket(stoppingToken);
        //await _obsService.HandleMessages();
        //await _obsService.SendRequest(req);
        await Task.WhenAll(
            _signalr.StartAsync(stoppingToken),
            _wordService.InsertValidCommands(),
            _wordService.InsertWords(),
            _twitchChatService.CreateWebSocket(stoppingToken));
        await Task.Run(() => _twitchChatService.HandleMessages());
    }
}
