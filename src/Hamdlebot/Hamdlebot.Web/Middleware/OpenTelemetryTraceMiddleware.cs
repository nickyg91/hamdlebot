using OpenTelemetry.Trace;

namespace Hamdlebot.Web.Middleware;

public class OpenTelemetryTraceMiddleware
{
    private readonly RequestDelegate _next;

    public OpenTelemetryTraceMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context, Tracer tracer)
    {
        using var span = tracer.StartActiveSpan(context.Request.Path); 
        await _next(context);
    }
}