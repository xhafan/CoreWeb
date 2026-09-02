using CoreBackend.Infrastructure;
using CoreBackend.Infrastructure.ErrorReporting;
using CoreBackend.TestsShared;
using CoreWeb.Features.Logs;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace CoreWeb.Tests.Features.Logs;

[TestFixture]
public class when_error_reporting_fails
{
    [Test]
    public async Task it_logs_the_delivery_failure_and_returns_ok()
    {
        var expectedException = new HttpRequestException("Slack unavailable");
        var reporter = A.Fake<IErrorReporter>();
        A.CallTo(() => reporter.Report(A<ErrorReport>._, A<CancellationToken>._))
            .Returns(Task.FromException(expectedException));
        var logger = new CapturingLogger<LogsController>();
        var iLoggingOptions = OptionsTestRetriever.Retrieve<LoggingOptions>();
        var httpContext = new DefaultHttpContext();
        var controller = new LogsController(logger, iLoggingOptions, reporter)
        {
            ControllerContext = new ControllerContext(new ActionContext(
                httpContext,
                new RouteData(),
                new ControllerActionDescriptor()
            ))
        };
        httpContext.Request.Headers[LogConstants.LogErrorApiSecretHeaderName]
            = iLoggingOptions.Value.LogErrorApiSecret;

        var result = await controller.LogError("JS error: broken interaction");

        result.ShouldBeOfType<OkResult>();
        logger.Entries.ShouldContain(entry =>
            entry.Level == LogLevel.Error &&
            entry.Exception == expectedException &&
            entry.Message.Contains(httpContext.TraceIdentifier, StringComparison.Ordinal)
        );
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);
}
