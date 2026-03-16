using Reinforced.Typings.Attributes;

namespace CoreWeb.Features.Logs;

[TsClass(IncludeNamespace = false, AutoExportMethods = false)]
public static class LogsUrlConstants
{
    [TsProperty(Constant = true, ShouldBeCamelCased = true)]
    public const string LogErrorUrl = "/api/Logs/LogError";
}