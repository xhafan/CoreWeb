using CoreBackend.Infrastructure;
using CoreBackend.Infrastructure.ErrorReporting;
using CoreBackend.TestsShared.Infrastructure.ErrorReporting;
using CoreBackend.TestsShared;
using CoreWeb.Features.Logs;
using CoreWeb.Infrastructure.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

namespace CoreWeb.Tests.Features.Logs;

[TestFixture]
public class when_preventing_duplicate_error_reports
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task it_reports_the_browser_error_once_even_when_delivery_fails(bool deliveryFails)
    {
        var queue = new ErrorReportQueue(Options.Create(new ErrorReportingOptions()));
        using var logger = new LoggerConfiguration().Enrich.FromLogContext()
            .WriteTo.Sink(new ErrorReportingSink(queue)).CreateLogger();
        var reporter = new RecordingReporter(deliveryFails);
        var loggingOptions = OptionsTestRetriever.Retrieve<LoggingOptions>();
        var context = new DefaultHttpContext();
        context.Request.Headers[LogConstants.LogErrorApiSecretHeaderName] = loggingOptions.Value.LogErrorApiSecret;
        var controller = new LogsController(new ForwardingLogger(logger), loggingOptions)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        IActionResult? result = null;
        var requestIdEnricher = new SerilogRequestIdEnricherMiddleware(_ =>
        {
            result = controller.LogError("Browser interaction failed");
            return Task.CompletedTask;
        });
        var beforeCallUtc = DateTime.UtcNow;
        await requestIdEnricher.Invoke(context);
        var afterCallUtc = DateTime.UtcNow;
        result.ShouldBeOfType<OkResult>();
        reporter.Reports.ShouldBeEmpty();

        using var delivery = new ErrorReportDeliveryBackgroundService(queue, reporter, ErrorReportTestServices.CreateGate());
        await delivery.StartAsync(CancellationToken.None);
        await delivery.StopAsync(CancellationToken.None);

        var report = reporter.Reports.ShouldHaveSingleItem();
        report.Message.ShouldBe("Browser interaction failed");
        report.Origin.ShouldBe(ErrorReportOrigin.Browser);
        report.RequestId.ShouldBe(context.TraceIdentifier);
        report.OccurredAtUtc.ShouldBeGreaterThanOrEqualTo(beforeCallUtc);
        report.OccurredAtUtc.ShouldBeLessThanOrEqualTo(afterCallUtc);
    }

    private sealed class RecordingReporter(bool deliveryFails) : IErrorReporter
    {
        public List<ErrorReport> Reports { get; } = [];
        public Task Report(ErrorReport report, CancellationToken cancellationToken = default)
        {
            Reports.Add(report);
            return deliveryFails ? Task.FromException(new HttpRequestException("Slack unavailable")) : Task.CompletedTask;
        }
    }

    private sealed class ForwardingLogger(Serilog.ILogger logger) : ILogger<LogsController>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => logger
            .ForContext("EventId", new { eventId.Id, eventId.Name }, destructureObjects: true).Write(
            logLevel == LogLevel.Error ? LogEventLevel.Error : LogEventLevel.Information,
            exception, "{Message:l}", formatter(state, exception));
    }
}
