namespace Hamdlebot.Web.Middleware.Extensions;

public static class OpenTelemetryTraceMiddlewareExtensions
{
    public static IApplicationBuilder UseOpenTelemetryTrace(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<OpenTelemetryTraceMiddleware>();
    }
}