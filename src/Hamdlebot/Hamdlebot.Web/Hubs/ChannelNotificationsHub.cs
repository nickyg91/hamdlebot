using Hamdlebot.Core.Models.Enums;
using Hamdlebot.Core.SignalR.Clients.ChannelNotifications;
using Microsoft.AspNetCore.SignalR;

namespace Hamdlebot.Web.Hubs;

public class ChannelNotificationsHub : Hub<IChannelNotificationsClient>
{
    public override async Task OnConnectedAsync()
    {
        if (Context.GetHttpContext() != null && Context.GetHttpContext()!.Request.Query.ContainsKey("twitchUserId"))
        {
            var channelId = Context.GetHttpContext()!.Request.Query["twitchUserId"];
            await Groups.AddToGroupAsync(Context.ConnectionId, channelId!);
        }
        await base.OnConnectedAsync();
    }
    
    public async Task SendChannelConnectionStatus(ChannelConnectionStatusType status, string twitchChannelId)
    {
        await Clients.Group(twitchChannelId).ReceiveChannelConnectionStatus(status);
    }
    
    public async Task SendObsConnectionStatus(ObsConnectionStatusType status, string twitchChannelId)
    {
        await Clients.Group(twitchChannelId).ReceiveObsConnectionStatus(status);
    }
}