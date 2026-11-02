using System.Text.RegularExpressions;

namespace CoreWeb.Features.Logs;

internal static partial class BrowserErrorAlertRules
{
    internal const string SuppressionReasonPropertyName = "BrowserErrorSuppressionReason";
    private const string CefSharpMissingUpdateObject = nameof(CefSharpMissingUpdateObject);

    internal static IReadOnlyList<ErrorReportMessageFilterRule> Rules { get; } =
        Array.AsReadOnly<ErrorReportMessageFilterRule>(
        [
            new(CefSharpMissingUpdateObjectPattern(), CefSharpMissingUpdateObject)
        ]);

    [GeneratedRegex("""\AJS error: \{"value":"Object Not Found Matching Id:[0-9]+, MethodName:update, ParamCount:4"\}\z""",
        RegexOptions.CultureInvariant)]
    private static partial Regex CefSharpMissingUpdateObjectPattern();
}
