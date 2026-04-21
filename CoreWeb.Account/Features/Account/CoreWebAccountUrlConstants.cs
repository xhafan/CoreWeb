using CoreBackend.Account.Features.Account;
using Reinforced.Typings.Attributes;

namespace CoreWeb.Account.Features.Account;

[TsClass(IncludeNamespace = false, AutoExportMethods = false)]
public static class CoreWebAccountUrlConstants
{
    [TsProperty(Constant = true, ShouldBeCamelCased = true)]
    public const string ReturnUrl = "returnUrl";

    [TsProperty(Constant = true, ShouldBeCamelCased = true)]
    public const string ApiGetLoginTokenConfirmationUrlTemplate = 
        $"/api/Login/GetLoginTokenConfirmation?loginTokenGuid={CoreBackendAccountUrlConstants.LoginTokenGuidVariable}";

    [TsProperty(Constant = true, ShouldBeCamelCased = true)]
    public const string ApiCompleteLoginUrlTemplate = 
        $"/api/CompleteLogin/CompleteLogin?loginTokenGuid={CoreBackendAccountUrlConstants.LoginTokenGuidVariable}";
}