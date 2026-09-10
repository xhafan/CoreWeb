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
}
