using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CoreBackend.Account.Features.Account;
using CoreBackend.Account.Features.Account.Domain;
using CoreBackend.Account.Features.Account.Queries;
using CoreBackend.Infrastructure.Validations;
using CoreDdd.Queries;
using CoreUtils;
using CoreWeb.Features.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CoreWeb.Account.Features.Account.LoginLinkSent;

[AllowAnonymous]
public abstract class BaseLoginLinkSentModel(
    IQueryExecutor queryExecutor,
    IOptions<JwtOptions> iJwtOptions
) : BasePageModel
{
    [BindProperty(SupportsGet = true)]
    [Required]
    [NotDefault]
    public Guid LoginTokenGuid { get; set; }

    public string Email { get; private set; } = null!;

    public async Task<IActionResult> OnGet()
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var loginTokenDto = await queryExecutor.ExecuteSingleAsync<GetLoginTokenByGuidQuery, LoginTokenDto>(new GetLoginTokenByGuidQuery(LoginTokenGuid));

        var claimsPrincipal = TokenValidator.ValidateToken(loginTokenDto.Token, iJwtOptions.Value);
        
        var email = claimsPrincipal.FindFirstValue(ClaimTypes.Email);
        Guard.Hope(!string.IsNullOrWhiteSpace(email), "Cannot get email from token.");
        Email = email;

        return Page();
    }
}