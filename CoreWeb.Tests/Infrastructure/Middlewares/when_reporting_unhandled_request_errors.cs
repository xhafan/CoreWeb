using System.Collections.Concurrent;
using CoreBackend.Infrastructure.ErrorReporting;
using CoreBackend.TestsShared.Infrastructure.ErrorReporting;
using CoreWeb.Infrastructure.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace CoreWeb.Tests.Infrastructure.Middlewares;

[TestFixture]
public class when_reporting_unhandled_request_errors
{
    [TestCase("Development")]
    [TestCase("Production")]
    public async Task each_failed_request_is_reported_once_without_suppressing_other_errors(string environment)
    {
        var queue = new ErrorReportQueue(Options.Create(new ErrorReportingOptions()));
        var capturedLogs = new CapturingSink();
        using var logger = new LoggerConfiguration().Enrich.FromLogContext()
            .WriteTo.Sink(capturedLogs)
            .WriteTo.Sink(new ErrorReportingSink(queue))
            .CreateLogger();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environment,
            ContentRootPath = AppContext.BaseDirectory
        });
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new ForwardingLoggerProvider(logger));
        await using var app = builder.Build();
        var pipeline = new ApplicationBuilder(app.Services);
        if (environment == "Development")
        {
            pipeline.UseDeveloperExceptionPage();
        }
        else
        {
            pipeline.UseExceptionHandler(errorApp => errorApp.Run(context =>
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return Task.CompletedTask;
            }));
        }
        pipeline.UseMiddleware<RequestLoggingMiddleware>();
        var bothRequestsStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var startedRequests = 0;
        pipeline.Run(async context =>
        {
            if (context.Request.Path == "/logged-error")
            {
                logger.Error("Independent application error");
                return;
            }
            if (Interlocked.Increment(ref startedRequests) == 2) bothRequestsStarted.SetResult();
            await bothRequestsStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            throw new InvalidOperationException($"Failure for {context.TraceIdentifier}");
        });
        var handler = pipeline.Build();
        var firstContext = CreateContext("first-request", "/first");
        var secondContext = CreateContext("second-request", "/second");
        await Task.WhenAll(Invoke(firstContext), Invoke(secondContext));
        await Invoke(CreateContext("logged-request", "/logged-error"));

        var reporter = new RecordingReporter();
        using var delivery = new ErrorReportDeliveryBackgroundService(queue, reporter, ErrorReportTestServices.CreateGate());
        await delivery.StartAsync(CancellationToken.None);
        await delivery.StopAsync(CancellationToken.None);

        reporter.Reports.Count.ShouldBe(3);
        foreach (var context in new[] { firstContext, secondContext })
        {
            context.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
            var report = reporter.Reports.Where(x => x.RequestId == context.TraceIdentifier).ShouldHaveSingleItem();
            report.Origin.ShouldBe(ErrorReportOrigin.Server);
            report.Message.ShouldContain($"Failure for {context.TraceIdentifier}");
            report.Message.ShouldNotContain("Request failed after");
            var diagnosticLogs = capturedLogs.Events.Where(x =>
                x.Level == LogEventLevel.Error && x.Exception?.Message == $"Failure for {context.TraceIdentifier}").ToArray();
            diagnosticLogs.Length.ShouldBe(2);
            diagnosticLogs.ShouldContain(x => x.RenderMessage().StartsWith("Request failed after", StringComparison.Ordinal));
        }
        reporter.Reports.Where(x => x.RequestId == "logged-request").ShouldHaveSingleItem()
            .Message.ShouldBe("Independent application error");

        DefaultHttpContext CreateContext(string requestId, string path)
        {
            var context = new DefaultHttpContext { TraceIdentifier = requestId, RequestServices = app.Services };
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = path;
            context.Request.Headers.Accept = "text/plain";
            context.Response.Body = new MemoryStream();
            return context;
        }

        Task Invoke(HttpContext context) => new SerilogRequestIdEnricherMiddleware(handler).Invoke(context);
    }

    private sealed class CapturingSink : ILogEventSink
    {
        public ConcurrentQueue<LogEvent> Events { get; } = new();
        public void Emit(LogEvent logEvent) => Events.Enqueue(logEvent);
    }

    private sealed class RecordingReporter : IErrorReporter
    {
        public List<ErrorReport> Reports { get; } = [];
        public Task Report(ErrorReport report, CancellationToken cancellationToken = default)
        {
            Reports.Add(report);
            return Task.CompletedTask;
        }
    }

    private sealed class ForwardingLoggerProvider(Serilog.ILogger logger) : ILoggerProvider
    {
        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) =>
            new ForwardingLogger(logger.ForContext("SourceContext", categoryName));
        public void Dispose() { }
    }

    private sealed class ForwardingLogger(Serilog.ILogger logger) : Microsoft.Extensions.Logging.ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => logger.Write(
                logLevel switch
                {
                    LogLevel.Error => LogEventLevel.Error,
                    LogLevel.Critical => LogEventLevel.Fatal,
                    LogLevel.Warning => LogEventLevel.Warning,
                    _ => LogEventLevel.Information
                }, exception, "{Message:l}", formatter(state, exception));
    }
}
