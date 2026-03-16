using MrWatchdog.Web.Infrastructure.Authorizations;

namespace MrWatchdog.Web.Infrastructure.RateLimiting;

public static class HttpContextUserIdOrClientIpAddressGetter
{
    public static string GetUserIdOrClientIpAddress(HttpContext httpContext)
    {
        return httpContext.User.IsAuthenticated()
            ? $"{httpContext.User.GetUserId()}"
            : $"[{httpContext.Connection.RemoteIpAddress}]";
    }
}
