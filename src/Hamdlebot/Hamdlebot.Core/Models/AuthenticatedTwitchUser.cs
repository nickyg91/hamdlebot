using System.Security.Claims;
using System.Security.Principal;

namespace Hamdlebot.Core.Models;

public class AuthenticatedTwitchUser :  IAuthenticatedTwitchUser
{
    public AuthenticatedTwitchUser(ClaimsPrincipal identity)
    {
        TwitchUserId = long.Parse(
            identity
                .Claims
                .FirstOrDefault(
                    x => x.Type.Contains("nameidentifier", StringComparison.CurrentCultureIgnoreCase))?.Value ?? "0"
        );
        TwitchUserName = identity
            .Claims
            .FirstOrDefault(
                x => x.Type.Contains("preferred_username", StringComparison.CurrentCultureIgnoreCase))?.Value ?? "";
    }

    public long TwitchUserId { get; }

    public string TwitchUserName { get; }
}