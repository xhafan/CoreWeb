using Reinforced.Typings.Attributes;

namespace CoreWeb.Account.Features.Shared;

[TsClass(IncludeNamespace = false)]
public static class CoreWebAccountStimulusControllers
{
    [TsProperty(Constant = true, ShouldBeCamelCased = true)]
    public const string AccountLoginLinkLoginLinkSent = "account-login-link--login-link-sent";

    [TsProperty(Constant = true, ShouldBeCamelCased = true)]
    public const string AccountLoginLinkLogin = "account-login-link--login";
}