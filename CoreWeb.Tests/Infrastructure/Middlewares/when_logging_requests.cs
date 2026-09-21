using CoreWeb.Infrastructure.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CoreWeb.Tests.Infrastructure.Middlewares;

[TestFixture]
public class when_logging_requests
{
    [TestCase("X-Sec-Purpose", "prefetch")]
    [TestCase("Purpose", "prefetch")]
    [TestCase("Turbo-Frame", "destination-tours")]
    public async Task request_start_log_contains_turbo_diagnostic_header(string headerName, string headerValue)
    {
        var messages = new List<string>();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/en/destinations/destination-1/prague";
        context.Request.Headers[headerName] = headerValue;
        var middleware = new RequestLoggingMiddleware(_ => Task.CompletedTask, new CapturingLogger(messages));

        await middleware.InvokeAsync(context);

        var requestStarted = messages.Single(message => message.StartsWith("Request started:", StringComparison.Ordinal));
        requestStarted.ShouldContain($"{headerName}={headerValue}");
    }

    [Test]
    public async Task completed_html_document_request_contains_dashboard_classification_fields()
    {
        var entries = new List<LogEntry>();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/en/destinations/destination-1/prague";
        context.Request.Headers.UserAgent = "Mozilla/5.0 Chrome/140.0";
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Destination details"));
        var middleware = new RequestLoggingMiddleware(
            httpContext =>
            {
                httpContext.Response.ContentType = "text/html; charset=utf-8";
                return Task.CompletedTask;
            },
            new CapturingStructuredLogger(entries)
        );

        await middleware.InvokeAsync(context);

        var completed = entries.Single(entry => entry.Message.StartsWith("Request completed:", StringComparison.Ordinal));
        completed.Properties["EndpointName"].ShouldBe("Destination details");
        completed.Properties["IsPageRequest"].ShouldBe(true);
        completed.Properties["TurboFrame"].ShouldBe(string.Empty);
        completed.Properties["BotClassification"].ShouldBe("NotBot");
    }

    [Test]
    public async Task completed_turbo_bot_request_is_not_classified_as_a_page_request()
    {
        var entries = new List<LogEntry>();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/en/destinations/destination-1/prague/tours";
        context.Request.Headers.UserAgent = "Mozilla/5.0 (compatible; Googlebot/2.1)";
        context.Request.Headers["Turbo-Frame"] = "destination-tours";
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Destination tours"));
        var middleware = new RequestLoggingMiddleware(
            httpContext =>
            {
                httpContext.Response.ContentType = "text/html";
                return Task.CompletedTask;
            },
            new CapturingStructuredLogger(entries)
        );

        await middleware.InvokeAsync(context);

        var completed = entries.Single(entry => entry.Message.StartsWith("Request completed:", StringComparison.Ordinal));
        completed.Properties["IsPageRequest"].ShouldBe(false);
        completed.Properties["TurboFrame"].ShouldBe("destination-tours");
        completed.Properties["BotClassification"].ShouldBe("Bot");
    }

    [Test]
    public async Task completed_request_without_user_agent_has_unknown_bot_classification()
    {
        var entries = new List<LogEntry>();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        var middleware = new RequestLoggingMiddleware(
            _ => Task.CompletedTask,
            new CapturingStructuredLogger(entries)
        );

        await middleware.InvokeAsync(context);

        var completed = entries.Single(entry => entry.Message.StartsWith("Request completed:", StringComparison.Ordinal));
        completed.Properties["BotClassification"].ShouldBe("Unknown");
    }

    private sealed class CapturingLogger(List<string> messages) : ILogger<RequestLoggingMiddleware>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            messages.Add(formatter(state, exception));
        }
    }

    private sealed class CapturingStructuredLogger(List<LogEntry> entries) : ILogger<RequestLoggingMiddleware>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            var properties = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values.ToDictionary(value => value.Key, value => value.Value)
                : new Dictionary<string, object?>();

            entries.Add(new LogEntry(formatter(state, exception), properties));
        }
    }

    private sealed record LogEntry(string Message, IReadOnlyDictionary<string, object?> Properties);
}
