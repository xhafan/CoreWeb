using CoreBackend.Infrastructure.Rebus;
using CoreBackend.Resources;
using Reinforced.Typings.Attributes;

namespace CoreWeb.Infrastructure.FrontendSettings;

[TsInterface(IncludeNamespace = false, AutoI = false)]
public class FrontendSettings
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global
    public required SharedTranslations SharedTranslations { get; set; }
    public required RebusOptions RebusOptions { get; set; }
    // ReSharper restore UnusedAutoPropertyAccessor.Global
}