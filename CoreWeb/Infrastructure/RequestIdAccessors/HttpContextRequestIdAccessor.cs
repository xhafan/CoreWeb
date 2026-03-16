using CoreBackend.Infrastructure.RequestIdAccessors;
using Microsoft.AspNetCore.Http;

namespace CoreWeb.Infrastructure.RequestIdAccessors;

public class HttpContextRequestIdAccessor(IHttpContextAccessor httpContextAccessor) : IRequestIdAccessor
{
    public string? GetRequestId()
    {
        return httpContextAccessor.HttpContext?.TraceIdentifier;
    }
}