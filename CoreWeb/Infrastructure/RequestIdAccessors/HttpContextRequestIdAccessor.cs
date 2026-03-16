using MrWatchdog.Core.Infrastructure.RequestIdAccessors;

namespace MrWatchdog.Web.Infrastructure.RequestIdAccessors;

public class HttpContextRequestIdAccessor(IHttpContextAccessor httpContextAccessor) : IRequestIdAccessor
{
    public string? GetRequestId()
    {
        return httpContextAccessor.HttpContext?.TraceIdentifier;
    }
}