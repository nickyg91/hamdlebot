using Hamdlebot.Core.Models.Logging;
using Hamdlebot.Core.SignalR.Clients.Logging;
using Microsoft.AspNetCore.SignalR;

namespace Hamdlebot.Web.Hubs;

public class BotLogHub : Hub<IBotLogClient>
{
    public async Task LogMessage(LogMessage message)
    {
        await Clients.All.LogMessage(message);
    }
    
    public async Task SendBotStatus(BotStatusType status)
    {
        await Clients.All.SendBotStatus(status);
    }
}