using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CoreBackend.Account.Features.LoginLink;
using CoreBackend.Account.Features.LoginLink.Commands;
using CoreBackend.Account.Features.LoginLink.Domain;
using CoreBackend.Account.Features.LoginLink.Queries;
using CoreBackend.Features.Jobs.Services;
using CoreBackend.Infrastructure.Rebus;
using CoreBackend.Infrastructure.Validations;
using CoreDdd.Queries;
using CoreUtils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CoreWeb.Account.Features.LoginLink.CompleteLogin;

[ApiController]
[Route("api/[controller]/[action]")]
[AllowAnonymous]
public abstract class BaseCompleteLoginController(
    ICoreBus bus,
    IJobCompletionAwaiter jobCompletionAwaiter,
    IQueryExecutor queryExecutor,
    IOptions<JwtOptions> iJwtOptions
) : ControllerBase
{
    protected abstract Task<long> _FetchOrCreateUser(string email, ClaimsPrincipal? tokenClaimsPrincipal = null);

    [HttpPost]
    public async Task<IActionResult> CompleteLogin([Required, NotDefault]Guid loginTokenGuid)
    {
        var loginTokenDto = await queryExecutor.ExecuteSingleAsync<GetLoginTokenByGuidQuery, LoginTokenDto>(new GetLoginTokenByGuidQuery(loginTokenGuid));

        Guard.Hope(loginTokenDto.Confirmed, "Login token has not been confirmed.");
        Guard.Hope(!loginTokenDto.Used, "Login token has been already used.");
        
        var tokenClaimsPrincipal = TokenValidator.ValidateToken(loginTokenDto.Token, iJwtOptions.Value);
        
        var email = tokenClaimsPrincipal.FindFirstValue(ClaimTypes.Email);
        Guard.Hope(!string.IsNullOrWhiteSpace(email), "Cannot get email from token.");

        var markLoginTokenAsUsedCommand = new MarkLoginTokenAsUsedCommand(loginTokenGuid);
        await bus.Send(markLoginTokenAsUsedCommand);
        await jobCompletionAwaiter.WaitForJobCompletion(markLoginTokenAsUsedCommand.Guid);

        await _FetchOrCreateUserAndLogUserIn(email, tokenClaimsPrincipal);

        var returnUrl = tokenClaimsPrincipal.FindFirstValue(CoreBackendClaimTypes.ReturnUrl);

        return Ok(
            !string.IsNullOrWhiteSpace(returnUrl)
                ? returnUrl
                : "/"
        );
    }

    private async Task _FetchOrCreateUserAndLogUserIn(string email, ClaimsPrincipal? tokenClaimsPrincipal = null)
    {
        var userId = await _FetchOrCreateUser(email, tokenClaimsPrincipal);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var cookieClaimsPrincipal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            cookieClaimsPrincipal,
            new AuthenticationProperties {IsPersistent = true}
        );
    }

    [HttpGet]
    public async Task<IActionResult> CompleteExternalLoginCallback(string? returnUrl = null)
    {
        var provider = User.Identity?.AuthenticationType;
        var isAuthenticatedByExternalProvider = User.Identity?.IsAuthenticated == true
                                                && provider != null
                                                && provider != CookieAuthenticationDefaults.AuthenticationScheme;
        if (!isAuthenticatedByExternalProvider)
        {
            return Unauthorized();
        }

        var email = User.FindFirstValue(ClaimTypes.Email);
        Guard.Hope(!string.IsNullOrWhiteSpace(email), $"Cannot get email from the external provider {provider}.");

        await _FetchOrCreateUserAndLogUserIn(email);

        return Redirect(
            !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : "/"
        );
    }
}