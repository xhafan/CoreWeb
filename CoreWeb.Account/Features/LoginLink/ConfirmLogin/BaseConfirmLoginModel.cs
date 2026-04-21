using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CoreBackend.Account.Features.Account;
using CoreBackend.Account.Features.Account.Commands;
using CoreBackend.Account.Features.Account.Domain;
using CoreBackend.Account.Features.Account.Queries;
using CoreBackend.Features.Jobs.Services;
using CoreBackend.Infrastructure.Rebus;
using CoreDdd.Queries;
using CoreUtils;
using CoreWeb.Features.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CoreWeb.Account.Features.Account.ConfirmLogin;

[AllowAnonymous]
public abstract class BaseConfirmLoginModel(
    ICoreBus bus,
    IOptions<JwtOptions> iJwtOptions,
    IQueryExecutor queryExecutor,
    IJobCompletionAwaiter jobCompletionAwaiter
) : BasePageModel
{
    [BindProperty(SupportsGet = true)]
    [Required]
    [StringLength(800)]
    public string LoginToken { get; set; } = null!;

    public string ReturnUrl { get; private set; } = null!;
    
    public async Task<IActionResult> OnGet()
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var loginTokenClaimsPrincipal = TokenValidator.ValidateToken(Uri.UnescapeDataString(LoginToken), iJwtOptions.Value);

        var loginTokenGuidString = loginTokenClaimsPrincipal.FindFirstValue(CoreBackendClaimTypes.Guid);
        Guard.Hope(!string.IsNullOrWhiteSpace(loginTokenGuidString), "Cannot get guid from token.");
        var loginTokenGuid = Guid.Parse(loginTokenGuidString);
        
        var loginTokenDto = await queryExecutor.ExecuteSingleAsync<GetLoginTokenByGuidQuery, LoginTokenDto>(new GetLoginTokenByGuidQuery(loginTokenGuid));
        Guard.Hope(!loginTokenDto.Confirmed, "Login token has already been confirmed.");
        
        var command = new ConfirmLoginTokenCommand(loginTokenGuid);
        await bus.Send(command);
        await jobCompletionAwaiter.WaitForJobCompletion(command.Guid);

        ReturnUrl = loginTokenClaimsPrincipal.FindFirstValue(CoreBackendClaimTypes.ReturnUrl) ?? "/";

        return Page();
    }
}