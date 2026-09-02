using CoreBackend.Infrastructure;
using CoreBackend.Infrastructure.ErrorReporting;
using CoreUtils;
using CoreWeb.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreWeb.Features.Logs;

[ApiController]
[EnableRateLimiting(RateLimitingConstants.LogErrorsRequestsPerSecondPerUserPolicy)]
[Route("api/[controller]/[action]")]
public class LogsController(
    ILogger<LogsController> logger,
    IOptions<LoggingOptions> iLoggingOptions,
    IErrorReporter errorReporter
) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [RequestSizeLimit(LogConstants.MaxLogMessageLength + 500)] // 500 bytes buffer
    public async Task<IActionResult> LogError([FromBody] string message)
    {
        Guard.Hope(!string.IsNullOrWhiteSpace(iLoggingOptions.Value.LogErrorApiSecret),
            $"{LogConstants.LoggingConfigurationSectionName}:{nameof(LoggingOptions.LogErrorApiSecret)} is not set.");

        var headers = HttpContext.Request.Headers;
        if (!headers.ContainsKey(LogConstants.LogErrorApiSecretHeaderName)
            || headers[LogConstants.LogErrorApiSecretHeaderName].ToString() != iLoggingOptions.Value.LogErrorApiSecret)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return BadRequest("Message cannot be empty.");
        }

        var safeMessage = message.Length > LogConstants.MaxLogMessageLength
            ? message.Substring(0, LogConstants.MaxLogMessageLength) + "…[truncated]"
            : message;

        logger.LogError(safeMessage);

        var errorReport = new ErrorReport(
            safeMessage,
            HttpContext.TraceIdentifier,
            DateTime.UtcNow
        );
        try
        {
            await errorReporter.Report(errorReport, HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "{ErrorReporterType} failed reporting frontend error. RequestId {RequestId}",
                errorReporter.GetType().Name,
                HttpContext.TraceIdentifier
            );
        }

        return Ok();
    }
}
