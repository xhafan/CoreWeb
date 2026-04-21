using System.ComponentModel.DataAnnotations;
using AspNetCore.ReCaptcha;
using CoreBackend.Account.Features;
using CoreBackend.Account.Features.LoginLink.Commands;
using CoreBackend.Account.Features.LoginLink.Domain;
using CoreBackend.Account.Features.LoginLink.Queries;
using CoreBackend.Features.Jobs.Queries;
using CoreBackend.Features.Jobs.Services;
using CoreBackend.Infrastructure.Rebus;
using CoreBackend.Infrastructure.Validations;
using CoreDdd.Queries;
using CoreUtils;
using CoreWeb.Features.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreWeb.Account.Features.LoginLink.Login;

[ValidateReCaptcha]
[AllowAnonymous]
public abstract class BaseLoginModel(
    ICoreBus bus,
    IJobCompletionAwaiter jobCompletionAwaiter,
    IQueryExecutor queryExecutor
) : BasePageModel
{
    [BindProperty(SupportsGet = true)]
    [StringLength(CoreBackendValidationConstants.UrlMaxLength)]
    public string? ReturnUrl { get; set; }
    
    // this field is here just to see ReCaptcha validation error
    public string Recaptcha { get; set; } = null!;

    protected abstract BaseSendLoginLinkToUserCommand GetSendLoginLinkToUserCommand();
    protected abstract string GetAccountLoginLinkSentUrl(Guid loginTokenGuid);

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return PageWithUnprocessableEntityStatus422();
        }

        var command = GetSendLoginLinkToUserCommand();
        await bus.Send(command);
        await jobCompletionAwaiter.WaitForJobCompletion(command.Guid);
        var commandGuid = command.Guid;

        var jobDto = (
            await queryExecutor.ExecuteAsync<GetJobQuery, JobDto>(new GetJobQuery(commandGuid))
        ).Single();
        var loginTokenId = jobDto.AffectedEntities.Single(x => x.EntityName == CoreBackendAccountDomainConstants.AccountLoginTokenEntityName).EntityId;
        var loginTokenDto = await queryExecutor.ExecuteSingleAsync<GetLoginTokenByIdQuery, LoginTokenDto>(new GetLoginTokenByIdQuery(loginTokenId));
        
        return Redirect(GetAccountLoginLinkSentUrl(loginTokenDto.Guid));
    }

    public IActionResult OnGetExternalLogin(string provider)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var redirectUrl = Url.Action("CompleteExternalLoginCallback", "CompleteLogin", values: new {ReturnUrl});
        Guard.Hope(redirectUrl != null, nameof(redirectUrl) + " is null");

        var properties = new AuthenticationProperties {RedirectUri = redirectUrl};
        if (!string.IsNullOrWhiteSpace(ReturnUrl))
        {
            properties.Items.Add(CoreWebAccountUrlConstants.ReturnUrl, ReturnUrl);
        }

        return new ChallengeResult(provider, properties);
    }
}