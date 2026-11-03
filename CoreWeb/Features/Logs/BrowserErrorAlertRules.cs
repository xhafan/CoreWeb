using System.Text.RegularExpressions;

namespace CoreWeb.Features.Logs;

internal static partial class BrowserErrorAlertRules
{
    internal const string SuppressionReasonPropertyName = "BrowserErrorSuppressionReason";
    private const string CefSharpMissingUpdateObject = nameof(CefSharpMissingUpdateObject);
    private const string PersistentAudioSourceFormatError = nameof(PersistentAudioSourceFormatError);

    internal static IReadOnlyList<ErrorReportMessageFilterRule> Rules { get; } =
        Array.AsReadOnly<ErrorReportMessageFilterRule>(
        [
            new(CefSharpMissingUpdateObjectPattern(), CefSharpMissingUpdateObject),
            new(PersistentAudioSourceFormatErrorPattern(), PersistentAudioSourceFormatError)
        ]);

    [GeneratedRegex("""\AJS error: \{"value":"Object Not Found Matching Id:[0-9]+, MethodName:update, ParamCount:4"\}\z""",
        RegexOptions.CultureInvariant)]
    private static partial Regex CefSharpMissingUpdateObjectPattern();

    [GeneratedRegex("""\AJS error: \{"message":"[^"\r\n]*","name":"NotSupportedError","component":"persistent-audio-player",[^{}\r\n]*"mediaErrorCode":4,[^{}\r\n]*"networkState":3,"readyState":0,"staleAttempt":false(?:,.*)?\}\z""",
        RegexOptions.CultureInvariant, 100)]
    private static partial Regex PersistentAudioSourceFormatErrorPattern();
}
