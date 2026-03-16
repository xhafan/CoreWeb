using CoreWeb.Infrastructure.Authorizations;
using Microsoft.AspNetCore.Http;

namespace CoreWeb.Infrastructure.RateLimiting;

public static class HttpContextUserIdOrClientIpAddressGetter
{
    public static string GetUserIdOrClientIpAddress(HttpContext httpContext)
    {
        return httpContext.User.IsAuthenticated()
            ? $"{httpContext.User.GetUserId()}"
            : $"[{httpContext.Connection.RemoteIpAddress}]";
    }
}
