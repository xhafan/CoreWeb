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
public class when_logging_error
{
    private ErrorReport? _reportedError;
    private DateTime _beforeCallUtc;
    private DateTime _afterCallUtc;
    private string _traceIdentifier = null!;
    private IErrorReporter _errorReporter = null!;

    [SetUp]
    public async Task Context()
    {
        var logger = A.Fake<ILogger<LogsController>>();
        _errorReporter = A.Fake<IErrorReporter>();
        A.CallTo(() => _errorReporter.Report(A<ErrorReport>._, A<CancellationToken>._))
            .Invokes((ErrorReport report, CancellationToken _) => _reportedError = report)
            .Returns(Task.CompletedTask);
        var iLoggingOptions = OptionsTestRetriever.Retrieve<LoggingOptions>();
        var httpContext = new DefaultHttpContext();
        _traceIdentifier = httpContext.TraceIdentifier;
        var logsController = new LogsController(logger, iLoggingOptions, _errorReporter)
        {
            ControllerContext = new ControllerContext(new ActionContext(
                httpContext,
                new RouteData(),
                new ControllerActionDescriptor()
            ))
        };
        httpContext.Request.Headers[LogConstants.LogErrorApiSecretHeaderName] = iLoggingOptions.Value.LogErrorApiSecret;

        _beforeCallUtc = DateTime.UtcNow;
        await logsController.LogError(
            """
            JS error: {"message":"Scraper web page name is missing.","name":"Error","source":"https://mrwatchdog_test/assets/bundle.p4xqk0duio.js","lineno":15,"colno":44916,"stack":"onScraperWebPageNameModified
            """
        );
        _afterCallUtc = DateTime.UtcNow;
    }

    [Test]
    public void the_error_is_reported()
    {
        _reportedError.ShouldNotBeNull();
        _reportedError.Message.ShouldContain("Scraper web page name is missing.");
        _reportedError.RequestId.ShouldBe(_traceIdentifier);
        _reportedError.OccurredAtUtc.ShouldBeGreaterThanOrEqualTo(_beforeCallUtc);
        _reportedError.OccurredAtUtc.ShouldBeLessThanOrEqualTo(_afterCallUtc);
        A.CallTo(() => _errorReporter.Report(A<ErrorReport>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
}
