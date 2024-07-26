using Hamdlebot.Core.Models.Enums;

namespace Hamdlebot.Core.SignalR.Clients.ChannelNotifications;

public interface IChannelNotificationsClient : ISignalrHubClient
{
    Task ReceiveChannelConnectionStatus(ChannelConnectionStatusType status);
    Task ReceiveObsConnectionStatus(ObsConnectionStatusType status);
}