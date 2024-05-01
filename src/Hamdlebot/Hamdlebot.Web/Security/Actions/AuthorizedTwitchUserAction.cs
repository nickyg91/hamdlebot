using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Hamdlebot.Web.Security.Actions;

public class AuthorizedTwitchUserAction : IAuthorizationFilter
{
    private readonly string[] _validUserIds = ["54975393", "978363539"];
    
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var userId = context.HttpContext.User.Claims.FirstOrDefault(x => x.Type.Contains("nameidentifier"))?.Value;
        if (userId == null || !_validUserIds.Contains(userId))
        {
            context.Result = new ForbidResult();
        }
    }
}