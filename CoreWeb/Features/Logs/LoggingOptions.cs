namespace CoreWeb.Features.Logs;

public class LoggingOptions
{
    public string LogErrorApiSecret { get; set; } = null!;
    public IReadOnlyList<ErrorReportMessageFilterRule> ErrorReportMessageFilters { get; set; } = [];
}