using Reinforced.Typings.Attributes;

namespace CoreWeb.Features.Shared;

[TsClass(IncludeNamespace = false)]
public static class CoreWebStimulusControllers
{
    [TsProperty(Constant = true, ShouldBeCamelCased = true)]
    public const string TurboFrame = "turbo-frame";

    [TsProperty(Constant = true, ShouldBeCamelCased = true)]
    public const string ViewOrEditForm = "view-or-edit-form";

    [TsProperty(Constant = true, ShouldBeCamelCased = true)]
    public const string Hint = "hint";

    [TsProperty(Constant = true, ShouldBeCamelCased = true)]
    public const string AccountLoginLinkSent = "account--login-link-sent";

    [TsProperty(Constant = true, ShouldBeCamelCased = true)]
    public const string AccountLogin = "account--login";
}