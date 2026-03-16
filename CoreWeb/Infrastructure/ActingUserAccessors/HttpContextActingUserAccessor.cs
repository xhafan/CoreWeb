using MrWatchdog.Core.Infrastructure.ActingUserAccessors;
using MrWatchdog.Web.Infrastructure.Authorizations;

namespace MrWatchdog.Web.Infrastructure.ActingUserAccessors;

public class HttpContextActingUserAccessor(IHttpContextAccessor httpContextAccessor) : IActingUserAccessor
{
    public long GetActingUserId()
    {
        return httpContextAccessor.HttpContext?.User.GetUserId() ?? 0;
    }
}