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

    [TestCase("GET", "text/html; charset=utf-8", "", true)]
    [TestCase("GET", "application/json", "", false)]
    [TestCase("HEAD", "text/html", "", false)]
    [TestCase("POST", "text/html", "", false)]
    [TestCase("GET", "text/html", "content", false)]
    public async Task page_eligibility_preserves_method_content_type_and_turbo_frame(string method, string contentType, string turboFrame, bool isPage)
    {
        var entries = new List<LogEntry>();
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Headers["Turbo-Frame"] = turboFrame;
        var middleware = new RequestLoggingMiddleware(httpContext =>
        {
            httpContext.Response.ContentType = contentType;
            return Task.CompletedTask;
        }, new CapturingStructuredLogger(entries));

        await middleware.InvokeAsync(context);

        _completed(entries).Properties["IsPageRequest"].ShouldBe(isPage);
    }

    [TestCase(200)]
    [TestCase(302)]
    [TestCase(404)]
    [TestCase(500)]
    public async Task bot_classification_is_logged_without_changing_the_response(int status)
    {
        var entries = new List<LogEntry>();
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Headers.UserAgent = "Googlebot/2.1";
        context.Response.Body = new MemoryStream();
        var middleware = new RequestLoggingMiddleware(async httpContext =>
        {
            httpContext.Response.StatusCode = status;
            httpContext.Response.ContentType = "text/html";
            await httpContext.Response.WriteAsync("unchanged");
        }, new CapturingStructuredLogger(entries));

        await middleware.InvokeAsync(context);

        var properties = _completed(entries).Properties;
        properties["StatusCode"].ShouldBe(status);
        properties["BotClassification"].ShouldBe("Bot");
        properties["IsPageRequest"].ShouldBe(true);
        context.Response.StatusCode.ShouldBe(status);
        context.Response.Body.Position = 0;
        (await new StreamReader(context.Response.Body).ReadToEndAsync()).ShouldBe("unchanged");
    }

    [TestCase("Purpose", "?__hsfp=value")]
    [TestCase("X-Sec-Purpose", "?_gl=value")]
    [TestCase("Sec-Purpose", "?__hssc=value&utm_source=email")]
    public async Task prefetch_and_tracking_parameters_do_not_classify_bots_or_exclude_pages(string header, string query)
    {
        var entries = new List<LogEntry>();
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/";
        context.Request.QueryString = new QueryString(query);
        context.Request.Headers.UserAgent = "Browser";
        context.Request.Headers[header] = "prefetch";
        var middleware = new RequestLoggingMiddleware(httpContext =>
        {
            httpContext.Response.ContentType = "text/html";
            return Task.CompletedTask;
        }, new CapturingStructuredLogger(entries));

        await middleware.InvokeAsync(context);

        _completed(entries).Properties["Path"].ShouldBe("/" + query);
        _completed(entries).Properties["IsPageRequest"].ShouldBe(true);
        _completed(entries).Properties["BotClassification"].ShouldBe("NotBot");
    }

    [Test]
    public async Task genuine_downstream_errors_are_logged_and_rethrown()
    {
        var entries = new List<LogEntry>();
        var failure = new InvalidOperationException("downstream failure");
        var middleware = new RequestLoggingMiddleware(_ => throw failure, new CapturingStructuredLogger(entries));
        (await Should.ThrowAsync<InvalidOperationException>(() => middleware.InvokeAsync(new DefaultHttpContext()))).ShouldBeSameAs(failure);
        entries.ShouldContain(entry => entry.Message.StartsWith("Request failed", StringComparison.Ordinal));
        entries.ShouldNotContain(entry => entry.Message.StartsWith("Request completed", StringComparison.Ordinal));
    }

    [TestCase("GoogleBoT/2.1", "Bot")]
    [TestCase("CustomCaSeCrAwLeR/1.0", "Bot")]
    [TestCase("CustomSpIdEr/1.0", "Bot")]
    [TestCase("SLURP/1.0", "Bot")]
    [TestCase("FACEBOOKEXTERNALHIT/1.1", "Bot")]
    [TestCase("CURL/8.0", "Bot")]
    [TestCase("WGET/1.0", "Bot")]
    [TestCase("PYTHON-REQUESTS/2.0", "Bot")]
    [TestCase("GO-HTTP-CLIENT/1.1", "Bot")]
    [TestCase("HEADLESSCHROME/140.0", "Bot")]
    [TestCase("POSTMANRUNTIME/7.0", "Bot")]
    [TestCase("AXIOS/1.0", "Bot")]
    [TestCase("OKHTTP/4.0", "Bot")]
    [TestCase("Mozilla/5.0 Chrome/140.0", "NotBot")]
    [TestCase("Mozilla/5.0 (iPhone) [FBAN/FBIOS;FBAV/1.0]", "NotBot")]
    [TestCase("Browser", "NotBot")]
    [TestCase(null, "Unknown")]
    [TestCase("", "Unknown")]
    [TestCase("   ", "Unknown")]
    public async Task completed_requests_use_case_insensitive_user_agent_keywords(string? userAgent, string classification)
    {
        var entries = new List<LogEntry>();
        var context = new DefaultHttpContext();
        if (userAgent != null)
        {
            context.Request.Headers.UserAgent = userAgent;
        }

        var middleware = new RequestLoggingMiddleware(_ => Task.CompletedTask, new CapturingStructuredLogger(entries));

        await middleware.InvokeAsync(context);

        _completed(entries).Properties["BotClassification"].ShouldBe(classification);
    }

    private static LogEntry _completed(List<LogEntry> entries) => entries.Single(entry => entry.Message.StartsWith("Request completed:", StringComparison.Ordinal));

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
