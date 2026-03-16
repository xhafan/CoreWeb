using System.Security.Claims;
using CoreUtils;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CoreWeb.Infrastructure.Authorizations;

public static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal claimsPrincipal)
    {
        public bool IsAuthenticated()
        {
            return claimsPrincipal.Identity?.IsAuthenticated == true
                   && claimsPrincipal.Identity?.AuthenticationType == CookieAuthenticationDefaults.AuthenticationScheme;
        }

        public long GetUserId()
        {
            if (!claimsPrincipal.IsAuthenticated())
            {
                return 0;
            }

            var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            Guard.Hope(userId != null, "Cannot retrieve userId");

            return long.Parse(userId);
        }
    }
}