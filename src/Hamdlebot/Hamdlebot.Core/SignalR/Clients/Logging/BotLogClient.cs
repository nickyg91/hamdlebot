using Hamdlebot.Core.Models.Logging;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Hamdlebot.Core.SignalR.Clients.Logging;

public class BotLogClient : IBotLogClient
{
    private readonly HubConnection _hub;
    public BotLogClient([FromKeyedServices("logHub")] HubConnection hub)
    {
        _hub = hub;
    }
    public async Task LogMessage(LogMessage message)
    {
        await _hub.InvokeAsync("LogMessage", message);
    }

    public async Task SendBotStatus(BotStatusType status)
    {
        await _hub.InvokeAsync("SendBotStatus", status);
    }
}