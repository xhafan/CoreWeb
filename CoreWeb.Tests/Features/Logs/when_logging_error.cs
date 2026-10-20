using CoreBackend.Infrastructure;
using CoreBackend.Infrastructure.ErrorReporting;
using CoreBackend.TestsShared;
using CoreWeb.Features.Logs;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreWeb.Tests.Features.Logs;

[TestFixture]
public class when_logging_error
{
    private ILogger<LogsController> _logger = null!;
    private IActionResult _result = null!;

    [SetUp]
    public void Context()
    {
        _logger = A.Fake<ILogger<LogsController>>();
        var iLoggingOptions = OptionsTestRetriever.Retrieve<LoggingOptions>();
        var httpContext = new DefaultHttpContext();
        var logsController = new LogsController(_logger, iLoggingOptions)
        {
            ControllerContext = new ControllerContext(new ActionContext(
                httpContext,
                new RouteData(),
                new ControllerActionDescriptor()
            ))
        };
        httpContext.Request.Headers[LogConstants.LogErrorApiSecretHeaderName] = iLoggingOptions.Value.LogErrorApiSecret;

        _result = logsController.LogError(
            """
            JS error: {"message":"Scraper web page name is missing.","name":"Error","source":"https://mrwatchdog_test/assets/bundle.p4xqk0duio.js","lineno":15,"colno":44916,"stack":"onScraperWebPageNameModified
            """
        );
    }

    [Test]
    public void the_error_is_logged_once_and_the_request_succeeds()
    {
        _result.ShouldBeOfType<OkResult>();
        var call = Fake.GetCalls(_logger)
            .Where(call => call.Method.Name == nameof(ILogger.Log)).ShouldHaveSingleItem();
        call.GetArgument<LogLevel>(0).ShouldBe(LogLevel.Error);
        call.GetArgument<EventId>(1).ShouldBe(ErrorReportingEventIds.BrowserError);
        var message = call.GetArgument<object>(2).ShouldNotBeNull().ToString();
        message.ShouldNotBeNull().ShouldContain("Scraper web page name is missing.");
    }

    [TestCase(null)]
    [TestCase("wrong-secret")]
    public async Task an_invalid_secret_returns_forbidden_without_an_authentication_scheme(string? secret)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication();
        await using var serviceProvider = services.BuildServiceProvider();
        var logger = A.Fake<ILogger<LogsController>>();
        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        if (secret != null) httpContext.Request.Headers[LogConstants.LogErrorApiSecretHeaderName] = secret;
        var controller = new LogsController(logger, Options.Create(new LoggingOptions { LogErrorApiSecret = "expected-secret" }))
        {
            ControllerContext = new ControllerContext(new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor()))
        };

        await controller.LogError("Unauthorized error").ExecuteResultAsync(controller.ControllerContext);

        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        Fake.GetCalls(logger).ShouldBeEmpty();
    }
}
