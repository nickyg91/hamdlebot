using Hamdlebot.Core.Models.Logging;
using Microsoft.AspNetCore.SignalR.Client;

namespace Hamdlebot.Core.SignalR.Clients.Logging;

public class BotLogClient : IBotLogClient
{
    private readonly HubConnection _hub;
    public BotLogClient(HubConnection hub)
    {
        _hub = hub;
    }
    public async Task LogMessage(LogMessage message)
    {
        await _hub.InvokeAsync("LogMessage", message);
    }
}