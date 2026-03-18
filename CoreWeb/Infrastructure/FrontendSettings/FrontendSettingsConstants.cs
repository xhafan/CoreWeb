using Reinforced.Typings.Attributes;

namespace CoreWeb.Infrastructure.FrontendSettings;

[TsClass(IncludeNamespace = false)]
public static class FrontendSettingsConstants
{
    [TsProperty(Constant = true, ShouldBeCamelCased = true)]
    public const string FrontendSettingsScriptId = "frontend-settings";
}