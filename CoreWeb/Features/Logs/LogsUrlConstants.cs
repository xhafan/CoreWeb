using Reinforced.Typings.Attributes;

namespace MrWatchdog.Web.Features.Logs;

[TsClass(IncludeNamespace = false, AutoExportMethods = false)]
public static class LogsUrlConstants
{
    [TsProperty(Constant = true, ShouldBeCamelCased = true)]
    public const string LogErrorUrl = "/api/Logs/LogError";
}