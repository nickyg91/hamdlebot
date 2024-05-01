using Hamdlebot.Web.Security.Actions;
using Microsoft.AspNetCore.Mvc;

namespace Hamdlebot.Web.Security.Attributes;

public class AuthorizedTwitchUserAttribute : TypeFilterAttribute
{
    public AuthorizedTwitchUserAttribute() : base(typeof(AuthorizedTwitchUserAction))
    {
    }
}