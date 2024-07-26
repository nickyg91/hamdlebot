using Hamdlebot.Core;
using Hamdlebot.Models.ViewModels;
using Microsoft.AspNetCore.SignalR.Client;

namespace Hamdlebot.Models;

public class TwitchChannelAggregate
{
    public TwitchChannelAggregate(string hamdlebotUserId, string clientId, string botAccessToken, ObsSettings? obsSettings, Channel channel, HubConnection hamdleHubConnection, HubConnection channelNotificationsConnection)
    {
        HamdlebotUserId = hamdlebotUserId;
        ClientId = clientId;
        BotAccessToken = botAccessToken;
        ObsSettings = obsSettings;
        Channel = channel;
        HamdleHubConnection = hamdleHubConnection;
        ChannelNotificationsConnection = channelNotificationsConnection;
    }
    
    public string HamdlebotUserId { get; set; }
    public string ClientId { get; set; }
    public string BotAccessToken { get; set; }
    public ObsSettings? ObsSettings { get; set; }
    public Channel Channel { get; set; }
    public HubConnection HamdleHubConnection { get; set; }
    public HubConnection ChannelNotificationsConnection { get; set; }
}