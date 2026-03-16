using CoreBackend.Infrastructure.ActingUserAccessors;
using CoreWeb.Infrastructure.Authorizations;
using Microsoft.AspNetCore.Http;

namespace CoreWeb.Infrastructure.ActingUserAccessors;

public class HttpContextActingUserAccessor(IHttpContextAccessor httpContextAccessor) : IActingUserAccessor
{
    public long GetActingUserId()
    {
        return httpContextAccessor.HttpContext?.User.GetUserId() ?? 0;
    }
}