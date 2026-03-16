using CoreBackend.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace CoreWeb.Infrastructure.Middlewares;

public class SerilogRequestIdEnricherMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext httpContext)
    {
        var requestId = httpContext.TraceIdentifier;
        using (Serilog.Context.LogContext.PushProperty(LogConstants.RequestId, requestId))
        {
            await next(httpContext);
        }
    }
}