using CoreDdd.Queries;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MrWatchdog.Core.Features.Account.Queries;
using System.ComponentModel.DataAnnotations;
using CoreBackend.Infrastructure.Validations;

namespace MrWatchdog.Web.Features.Account;

[ApiController]
[Route("api/[controller]/[action]")]
public class LoginController(IQueryExecutor queryExecutor) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<bool> GetLoginTokenConfirmation([Required, NotDefault]Guid loginTokenGuid)
    {
        return await queryExecutor.ExecuteSingleAsync<GetLoginTokenConfirmationQuery, bool>(new GetLoginTokenConfirmationQuery(loginTokenGuid));
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Redirect("/");
    }
}