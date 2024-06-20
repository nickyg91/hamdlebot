namespace Hamdlebot.Core.Models;

public interface IAuthenticatedTwitchUser
{
    long TwitchUserId { get; }
    string TwitchUserName { get; }
}