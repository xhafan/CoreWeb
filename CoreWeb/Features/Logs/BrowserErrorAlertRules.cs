using System.Text.RegularExpressions;

namespace CoreWeb.Features.Logs;

internal static partial class BrowserErrorAlertRules
{
    internal const string SuppressionReasonPropertyName = "BrowserErrorSuppressionReason";
    private const string CefSharpMissingUpdateObject = nameof(CefSharpMissingUpdateObject);
    private const string PersistentAudioSourceFormatError = nameof(PersistentAudioSourceFormatError);
    private const string PersistentAudioStaleSourceNotSupported = nameof(PersistentAudioStaleSourceNotSupported);
    private const string OpaqueScriptError = nameof(OpaqueScriptError);
    private const string BraveWalletMissingEthereumProvider = nameof(BraveWalletMissingEthereumProvider);

    internal static IReadOnlyList<ErrorReportMessageFilterRule> Rules { get; } =
        Array.AsReadOnly<ErrorReportMessageFilterRule>(
        [
            new(CefSharpMissingUpdateObjectPattern(), CefSharpMissingUpdateObject),
            new(PersistentAudioSourceFormatErrorPattern(), PersistentAudioSourceFormatError),
            new(PersistentAudioStaleSourceNotSupportedPattern(), PersistentAudioStaleSourceNotSupported),
            new(OpaqueScriptErrorPattern(), OpaqueScriptError),
            new(BraveWalletMissingEthereumProviderPattern(), BraveWalletMissingEthereumProvider)
        ]);

    [GeneratedRegex("""\AJS error: \{"value":"Object Not Found Matching Id:[0-9]+, MethodName:update, ParamCount:4"\}\z""",
        RegexOptions.CultureInvariant)]
    private static partial Regex CefSharpMissingUpdateObjectPattern();

    [GeneratedRegex("""\AJS error: \{"value":"Script error\.","source":"","lineno":0,"colno":0\}\z""",
        RegexOptions.CultureInvariant)]
    private static partial Regex OpaqueScriptErrorPattern();

    [GeneratedRegex("""\AJS error: \{"message":"undefined is not an object \(evaluating 'window\.ethereum\.selectedAddress = undefined'\)","name":"TypeError","source":"(?<source>https?://[^"\\\r\n]+)","lineno":1,"colno":16,"stack":"global code \(\k<source>:1:16\)"\}\z""",
        RegexOptions.CultureInvariant, 100)]
    private static partial Regex BraveWalletMissingEthereumProviderPattern();

    [GeneratedRegex("""\AJS error: \{"message":"[^"\r\n]*","name":"NotSupportedError","component":"persistent-audio-player",[^{}\r\n]*"mediaErrorCode":4,[^{}\r\n]*"networkState":3,"readyState":0,"staleAttempt":false(?:,.*)?\}\z""",
        RegexOptions.CultureInvariant, 100)]
    private static partial Regex PersistentAudioSourceFormatErrorPattern();

    [GeneratedRegex("""\AJS error: \{"message":"(?:[^"\\\r\n]|\\(?:["\\/bfnrt]|u[0-9A-Fa-f]{4}))*","name":"NotSupportedError","component":"persistent-audio-player","playbackAction":"(?:request|toggle)","mediaId":"[^"\\\r\n]+","mediaUrl":"https?://[^"\\\r\n]+","staleAttempt":true(?:,"stack":"(?:[^"\\\r\n]|\\(?:["\\/bfnrt]|u[0-9A-Fa-f]{4}))*")?\}\z""",
        RegexOptions.CultureInvariant, 100)]
    private static partial Regex PersistentAudioStaleSourceNotSupportedPattern();
}
