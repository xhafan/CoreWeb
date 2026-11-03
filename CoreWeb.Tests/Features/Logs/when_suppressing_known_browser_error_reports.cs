using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using CoreBackend.Infrastructure;
using CoreBackend.Infrastructure.ErrorReporting;
using CoreBackend.TestsShared.Infrastructure.ErrorReporting;
using CoreWeb.Features.Logs;
using CoreWeb.Infrastructure.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Core.Enrichers;
using Serilog.Events;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace CoreWeb.Tests.Features.Logs;

[TestFixture]
public class when_suppressing_known_browser_error_reports
{
    private const string KnownMessage = """JS error: {"value":"Object Not Found Matching Id:1, MethodName:update, ParamCount:4"}""";
    private const string OpaqueScriptError = """JS error: {"value":"Script error.","source":"","lineno":0,"colno":0}""";
    private const string KnownAudioMessage = """JS error: {"message":"Failed to load because no supported source was found.","name":"NotSupportedError","component":"persistent-audio-player","playbackAction":"request","mediaId":"1374527","mediaUrl":"https://d3f6ixrx8a9kmw.cloudfront.net/data/v4/pois/554e48f9-d47e-4cf8-b58c-b65d1c7cafe9/resources/1374527.mp3","currentSrc":"https://d3f6ixrx8a9kmw.cloudfront.net/data/v4/pois/554e48f9-d47e-4cf8-b58c-b65d1c7cafe9/resources/1374527.mp3","mediaErrorCode":4,"mediaErrorMessage":"MEDIA_ELEMENT_ERROR: Format error","networkState":3,"readyState":0,"staleAttempt":false}""";
    private const string ReasonPropertyName = "BrowserErrorSuppressionReason";

    [Test]
    public async Task it_keeps_the_raw_opaque_script_error_without_delivering_an_alert()
    {
        await using var pipeline = new TestPipeline();

        (await pipeline.LogBrowserAsync(OpaqueScriptError)).ShouldBeOfType<OkResult>();
        await pipeline.DrainAsync();

        pipeline.Reports.ShouldBeEmpty();
        var logEvent = pipeline.Events.ShouldHaveSingleItem();
        logEvent.Level.ShouldBe(LogEventLevel.Error);
        logEvent.RenderMessage().ShouldBe(OpaqueScriptError);
        logEvent.Properties[ErrorReportingLogPolicy.SuppressPropertyName].ShouldBeOfType<ScalarValue>().Value.ShouldBe(true);
        logEvent.Properties[ReasonPropertyName].ShouldBeOfType<ScalarValue>().Value.ShouldBe("OpaqueScriptError");
    }

    [TestCase("Script error.", "Script error?")]
    [TestCase("\"source\":\"\"", "\"source\":\"https://cdn.example.com/script.js\"")]
    [TestCase("\"lineno\":0", "\"lineno\":1")]
    [TestCase("\"colno\":0", "\"colno\":1")]
    [TestCase("JS error: ", "")]
    public async Task it_reports_script_errors_with_other_messages_or_source_details(string original, string replacement)
    {
        await using var pipeline = new TestPipeline();
        var message = OpaqueScriptError.Replace(original, replacement);

        (await pipeline.LogBrowserAsync(message)).ShouldBeOfType<OkResult>();
        await pipeline.DrainAsync();

        pipeline.Reports.ShouldHaveSingleItem().Message.ShouldStartWith(message);
        AssertUnsuppressed(pipeline.Events.ShouldHaveSingleItem());
    }

    [TestCaseSource(nameof(MatchingAudioMessages))]
    public async Task it_keeps_the_raw_audio_format_error_without_delivering_an_alert(string message)
    {
        await using var pipeline = new TestPipeline();

        (await pipeline.LogBrowserAsync(message)).ShouldBeOfType<OkResult>();
        await pipeline.DrainAsync();

        pipeline.Reports.ShouldBeEmpty();
        var logEvent = pipeline.Events.ShouldHaveSingleItem();
        logEvent.Level.ShouldBe(LogEventLevel.Error);
        logEvent.RenderMessage().ShouldBe(message);
        logEvent.Properties[ErrorReportingLogPolicy.SuppressPropertyName].ShouldBeOfType<ScalarValue>().Value.ShouldBe(true);
        logEvent.Properties[ReasonPropertyName].ShouldBeOfType<ScalarValue>().Value.ShouldBe("PersistentAudioSourceFormatError");
    }

    [TestCaseSource(nameof(UnmatchedAudioMessages))]
    public async Task it_reports_other_audio_failures_and_unexpected_envelopes(string message)
    {
        await using var pipeline = new TestPipeline();

        (await pipeline.LogBrowserAsync(message)).ShouldBeOfType<OkResult>();
        await pipeline.DrainAsync();

        pipeline.Reports.ShouldHaveSingleItem().Message.ShouldStartWith(message);
        AssertUnsuppressed(pipeline.Events.ShouldHaveSingleItem());
    }

    [TestCase("1")]
    [TestCase("42")]
    public async Task it_keeps_the_raw_browser_error_but_does_not_deliver_a_known_cefsharp_report(string id)
    {
        await using var pipeline = new TestPipeline();
        var message = KnownMessage.Replace("Id:1,", $"Id:{id},");

        (await pipeline.LogBrowserAsync(message)).ShouldBeOfType<OkResult>();
        await pipeline.DrainAsync();

        pipeline.Reports.ShouldBeEmpty();
        var logEvent = pipeline.Events.ShouldHaveSingleItem();
        logEvent.Level.ShouldBe(LogEventLevel.Error);
        logEvent.RenderMessage().ShouldBe(message);
        logEvent.Properties[LogConstants.RequestId].ShouldBeOfType<ScalarValue>().Value.ShouldBe("browser-request");
        logEvent.Properties[ErrorReportingLogPolicy.SuppressPropertyName].ShouldBeOfType<ScalarValue>().Value.ShouldBe(true);
        logEvent.Properties[ReasonPropertyName].ShouldBeOfType<ScalarValue>().Value.ShouldBe("CefSharpMissingUpdateObject");
        var eventId = logEvent.Properties["EventId"].ShouldBeOfType<StructureValue>();
        eventId.Properties.Single(property => property.Name == nameof(EventId.Id)).Value
            .ShouldBeOfType<ScalarValue>().Value.ShouldBe(ErrorReportingEventIds.BrowserError.Id);
        eventId.Properties.Single(property => property.Name == nameof(EventId.Name)).Value
            .ShouldBeOfType<ScalarValue>().Value.ShouldBe(ErrorReportingEventIds.BrowserError.Name);
    }

    [TestCaseSource(nameof(UnmatchedMessages))]
    public async Task it_reports_messages_that_do_not_match_the_complete_cefsharp_rule(string message)
    {
        await using var pipeline = new TestPipeline();

        (await pipeline.LogBrowserAsync(message)).ShouldBeOfType<OkResult>();
        await pipeline.DrainAsync();

        var report = pipeline.Reports.ShouldHaveSingleItem();
        report.Message.ShouldStartWith(message);
        report.Origin.ShouldBe(ErrorReportOrigin.Browser);
        report.RequestId.ShouldBe("browser-request");
        var logEvent = pipeline.Events.ShouldHaveSingleItem();
        logEvent.RenderMessage().ShouldBe(message);
        AssertUnsuppressed(logEvent);
    }

    [Test]
    public async Task it_preserves_truncation_and_reports_an_oversized_envelope()
    {
        await using var pipeline = new TestPipeline();
        var message = "JS error: {\"value\":\"" + new string('x', LogConstants.MaxLogMessageLength) + "\"}";

        (await pipeline.LogBrowserAsync(message)).ShouldBeOfType<OkResult>();
        await pipeline.DrainAsync();

        var logEvent = pipeline.Events.ShouldHaveSingleItem();
        var loggedMessage = logEvent.RenderMessage();
        loggedMessage.ShouldEndWith("…[truncated]");
        loggedMessage.Length.ShouldBe(LogConstants.MaxLogMessageLength + "…[truncated]".Length);
        AssertUnsuppressed(logEvent);
        pipeline.Reports.ShouldHaveSingleItem().Message.ShouldStartWith(loggedMessage);
    }

    [TestCase(null, KnownMessage, StatusCodes.Status403Forbidden)]
    [TestCase("wrong-secret", KnownMessage, StatusCodes.Status403Forbidden)]
    [TestCase("test-secret", "", StatusCodes.Status400BadRequest)]
    [TestCase("test-secret", "   ", StatusCodes.Status400BadRequest)]
    public async Task it_preserves_authentication_and_empty_message_validation(string? secret, string message, int expectedStatus)
    {
        await using var pipeline = new TestPipeline();

        var result = await pipeline.LogBrowserAsync(message, secret);
        ((IStatusCodeActionResult)result).StatusCode.ShouldBe(expectedStatus);
        await pipeline.DrainAsync();

        pipeline.Events.ShouldBeEmpty();
        pipeline.Reports.ShouldBeEmpty();
    }

    [TestCase(KnownMessage)]
    [TestCase(KnownAudioMessage)]
    public async Task it_does_not_leak_suppression_to_other_errors(string suppressedMessage)
    {
        await using var pipeline = new TestPipeline();
        await pipeline.LogBrowserAsync(suppressedMessage);
        await pipeline.LogBrowserAsync("Ordinary browser failure");
        pipeline.LogServer(suppressedMessage);
        await pipeline.DrainAsync();

        pipeline.Reports.Count.ShouldBe(2);
        pipeline.Reports.Single(report => report.Origin == ErrorReportOrigin.Browser).Message
            .ShouldStartWith("Ordinary browser failure");
        pipeline.Reports.Single(report => report.Origin == ErrorReportOrigin.Server).Message.ShouldStartWith(suppressedMessage);
        var subsequentEvents = pipeline.Events.Skip(1).ToArray();
        subsequentEvents.Length.ShouldBe(2);
        foreach (var logEvent in subsequentEvents) AssertUnsuppressed(logEvent);
    }

    private static void AssertUnsuppressed(LogEvent logEvent)
    {
        logEvent.Level.ShouldBe(LogEventLevel.Error);
        logEvent.Properties.ShouldNotContainKey(ErrorReportingLogPolicy.SuppressPropertyName);
        logEvent.Properties.ShouldNotContainKey(ReasonPropertyName);
    }

    private static IEnumerable<string> MatchingAudioMessages()
    {
        yield return KnownAudioMessage;
        yield return KnownAudioMessage.Replace("1374527", "different-track").Replace(".mp3", ".wav?x=1&y=2");
        yield return KnownAudioMessage.Replace("\"request\"", "\"toggle\"");
        yield return KnownAudioMessage.Replace("Failed to load because no supported source was found.", "The element has no supported sources.");
        yield return KnownAudioMessage.Replace("MEDIA_ELEMENT_ERROR: Format error", "PipelineStatus::DEMUXER_ERROR_COULD_NOT_OPEN: FFmpegDemuxer: open context failed");
        yield return KnownAudioMessage[..^1] + """, "stack":"Error: failed\n at play (https://example.com/player.js:1:2)","context":"{\"name\":\"other\"}"}""";
    }

    private static IEnumerable<string> UnmatchedAudioMessages()
    {
        yield return KnownAudioMessage.Replace("NotSupportedError", "NotAllowedError");
        yield return KnownAudioMessage.Replace("persistent-audio-player", "poi-video");
        yield return KnownAudioMessage.Replace("\"mediaErrorCode\":4", "\"mediaErrorCode\":3");
        yield return KnownAudioMessage.Replace("\"mediaErrorCode\":4", "\"mediaErrorCode\":2");
        yield return KnownAudioMessage.Replace("\"mediaErrorCode\":4", "\"mediaErrorCode\":40");
        yield return KnownAudioMessage.Replace("\"mediaErrorCode\":4", "\"mediaErrorCode\":\"4\"");
        yield return KnownAudioMessage.Replace("\"networkState\":3", "\"networkState\":2");
        yield return KnownAudioMessage.Replace("\"readyState\":0", "\"readyState\":1");
        yield return KnownAudioMessage.Replace("\"staleAttempt\":false", "\"staleAttempt\":true");
        yield return "prefix " + KnownAudioMessage;
        yield return KnownAudioMessage + " suffix";
        yield return KnownAudioMessage + "\n";
        yield return KnownAudioMessage[..^1];
        yield return "JS error: {\"details\":" + KnownAudioMessage["JS error: ".Length..] + "}";
        yield return "JS error: {\"value\":" + JsonSerializer.Serialize(KnownAudioMessage) + "}";
        var reorderedPayload = JsonNode.Parse(KnownAudioMessage["JS error: ".Length..])!.AsObject();
        var reordered = new JsonObject(reorderedPayload.Reverse().Select(pair => KeyValuePair.Create(pair.Key, pair.Value?.DeepClone())));
        yield return "JS error: " + reordered.ToJsonString();
        yield return "JS error: " + reordered.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        foreach (var field in new[] { "name", "component", "mediaErrorCode", "networkState", "readyState", "staleAttempt" })
        {
            var payload = JsonNode.Parse(KnownAudioMessage["JS error: ".Length..])!.AsObject();
            payload.Remove(field);
            yield return "JS error: " + payload.ToJsonString();
        }
    }

    private static IEnumerable<string> UnmatchedMessages()
    {
        yield return KnownMessage.Replace("update", "delete");
        yield return KnownMessage.Replace("ParamCount:4", "ParamCount:3");
        yield return KnownMessage.Replace("Id:1", "Id:");
        yield return KnownMessage.Replace("Id:1", "Id:abc");
        yield return KnownMessage.Replace("Id:1", "Id:١");
        yield return "prefix " + KnownMessage;
        yield return KnownMessage + " suffix";
        yield return KnownMessage + "\n";
        yield return KnownMessage + "\r\n";
        yield return KnownMessage.Replace("update", "Update");
        yield return KnownMessage.Replace("JS error:", "JS Error:");
        yield return KnownMessage.Replace("Id:1, ", "Id:1,");
        yield return KnownMessage.Replace("\"value\":", "\"value\": ");
        yield return KnownMessage.Replace("\"value\"", "\"message\"");
        yield return KnownMessage.Replace("\"value\"", "\"Value\"");
        yield return KnownMessage.Replace("{\"value\":", "{\"value\":\"other\",\"value\":");
        yield return KnownMessage.Replace("\"}", "\",\"extra\":true}");
        yield return KnownMessage.Replace("\"}", "\",\"message\":\"Actual error\",\"name\":\"Error\",\"stack\":\"trace\"}");
        yield return "Object Not Found Matching Id:1, MethodName:update, ParamCount:4";
        yield return "Ordinary failure";
        yield return KnownMessage[..^1];
        yield return "JS error: null";
        yield return "JS error: []";
        yield return "JS error: {\"value\":17}";
        yield return "JS error: {\"value\":{\"message\":\"Object Not Found Matching Id:1, MethodName:update, ParamCount:4\"}}";
        yield return "JS error: {\"error\":{\"value\":\"Object Not Found Matching Id:1, MethodName:update, ParamCount:4\"}}";
    }

    private sealed class TestPipeline : IAsyncDisposable
    {
        private readonly Serilog.Core.Logger _serilog;
        private readonly ILoggerFactory _loggerFactory;
        private readonly RecordingReporter _reporter = new();
        private readonly ErrorReportDeliveryBackgroundService _delivery;
        private bool _drained;

        public ConcurrentQueue<LogEvent> Events { get; } = new();
        public IReadOnlyCollection<ErrorReport> Reports => _reporter.Reports;

        public TestPipeline()
        {
            var queue = new ErrorReportQueue(Options.Create(new ErrorReportingOptions()));
            _serilog = new LoggerConfiguration().Enrich.FromLogContext()
                .WriteTo.Sink(new RecordingSink(Events))
                .WriteTo.Sink(new ErrorReportingSink(queue)).CreateLogger();
            _loggerFactory = LoggerFactory.Create(logging => logging.AddProvider(new ForwardingLoggerProvider(_serilog)));
            _delivery = new ErrorReportDeliveryBackgroundService(queue, _reporter, ErrorReportTestServices.CreateGate());
        }

        public async Task<IActionResult> LogBrowserAsync(string message, string? secret = "test-secret")
        {
            var context = new DefaultHttpContext { TraceIdentifier = "browser-request" };
            if (secret != null) context.Request.Headers[LogConstants.LogErrorApiSecretHeaderName] = secret;
            var controller = new LogsController(_loggerFactory.CreateLogger<LogsController>(),
                Options.Create(new LoggingOptions { LogErrorApiSecret = "test-secret" }))
            {
                ControllerContext = new ControllerContext { HttpContext = context }
            };
            IActionResult? result = null;
            var middleware = new SerilogRequestIdEnricherMiddleware(_ =>
            {
                result = controller.LogError(message);
                return Task.CompletedTask;
            });
            await middleware.Invoke(context);
            return result.ShouldNotBeNull();
        }

        public void LogServer(string message) => _loggerFactory.CreateLogger<TestPipeline>().LogError(message);

        public async Task DrainAsync()
        {
            if (_drained) return;
            await _delivery.StartAsync(CancellationToken.None);
            await _delivery.StopAsync(CancellationToken.None);
            _drained = true;
        }

        public async ValueTask DisposeAsync()
        {
            await DrainAsync();
            _delivery.Dispose();
            _loggerFactory.Dispose();
            await _serilog.DisposeAsync();
        }
    }

    private sealed class RecordingSink(ConcurrentQueue<LogEvent> events) : ILogEventSink
    {
        public void Emit(LogEvent logEvent) => events.Enqueue(logEvent);
    }

    private sealed class ForwardingLoggerProvider(Serilog.ILogger logger) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new ForwardingLogger(logger.ForContext("SourceContext", categoryName));
        public void Dispose() { }
    }

    private sealed class ForwardingLogger(Serilog.ILogger logger) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            state is IEnumerable<KeyValuePair<string, object?>> properties
                ? LogContext.Push(properties.Select(property => new PropertyEnricher(property.Key, property.Value)).ToArray())
                : null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => logger
            .ForContext("EventId", new { eventId.Id, eventId.Name }, destructureObjects: true)
            .Write((LogEventLevel)logLevel, exception, "{Message:l}", formatter(state, exception));
    }

    private sealed class RecordingReporter : IErrorReporter
    {
        public ConcurrentQueue<ErrorReport> Reports { get; } = new();

        public Task Report(ErrorReport report, CancellationToken cancellationToken = default)
        {
            Reports.Enqueue(report);
            return Task.CompletedTask;
        }
    }
}
