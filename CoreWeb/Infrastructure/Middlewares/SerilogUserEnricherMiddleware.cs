using System.Security.Claims;
using CoreBackend.Infrastructure;
using CoreWeb.Infrastructure.Authorizations;
using Microsoft.AspNetCore.Http;

namespace CoreWeb.Infrastructure.Middlewares;

public class SerilogUserEnricherMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext httpContext)
    {
        var userId = httpContext.User.IsAuthenticated()
            ? long.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!)
            : 0;

        using (Serilog.Context.LogContext.PushProperty(LogConstants.UserId, $"{userId}"))
        {
            await next(httpContext);
        }
    }
}