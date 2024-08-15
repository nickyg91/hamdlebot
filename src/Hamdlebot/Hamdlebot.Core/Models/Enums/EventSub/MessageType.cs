namespace Hamdlebot.Core.Models.Enums.EventSub;

public enum MessageType : byte
{
    SessionWelcome = 1,
    SessionKeepalive = 2,
    Notification = 3,
    SessionReconnect = 4,
    Revocation = 5,
}