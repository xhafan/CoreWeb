using System.Text.RegularExpressions;

namespace CoreWeb.Features.Logs;

public sealed record ErrorReportMessageFilterRule(Regex Pattern, string Reason);

public static class ErrorReportMessageFilter
{
    public const string SuppressionReasonPropertyName = "BrowserErrorSuppressionReason";

    public static string? GetSuppressionReason(string message, IReadOnlyList<ErrorReportMessageFilterRule> rules)
    {
        foreach (var rule in rules)
        {
            if (rule.Pattern.IsMatch(message)) return rule.Reason;
        }

        return null;
    }
}
