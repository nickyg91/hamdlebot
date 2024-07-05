using Hamdlebot.Core;
using Hamdlebot.Models.Enums;
using Hamdlebot.Models.ViewModels;

namespace Hamdlebot.Models;

public class TwitchChannelUpdateMessage
{
    public ActionType Action { get; set; }
    public ObsSettings? ObsSettings { get; set; }
    public Channel? Channel { get; set; }
}