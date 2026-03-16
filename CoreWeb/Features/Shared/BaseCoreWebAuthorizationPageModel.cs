using CoreWeb.Infrastructure.Authorizations;
using Microsoft.AspNetCore.Authorization;

namespace CoreWeb.Features.Shared;

public abstract class BaseCoreWebAuthorizationPageModel(IAuthorizationService authorizationService) : BasePageModel
{
    protected async Task<bool> IsAuthorizedAsSuperAdmin()
    {
        var result = await authorizationService.AuthorizeAsync(
            User, 
            null, 
            new SuperAdminRequirement()
        );
        return result.Succeeded;
    }
}