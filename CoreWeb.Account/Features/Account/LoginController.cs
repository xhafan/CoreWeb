using System.ComponentModel.DataAnnotations;
using CoreBackend.Account.Features.Account.Queries;
using CoreBackend.Infrastructure.Validations;
using CoreDdd.Queries;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreWeb.Account.Features.Account;

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