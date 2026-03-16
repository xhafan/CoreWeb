using CoreUtils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using MrWatchdog.Core.Infrastructure;
using MrWatchdog.Core.Infrastructure.EmailSenders;
using MrWatchdog.Core.Infrastructure.Rebus;
using MrWatchdog.Web.Infrastructure.RateLimiting;

namespace MrWatchdog.Web.Features.Logs;

[ApiController]
[EnableRateLimiting(RateLimitingConstants.LogErrorsRequestsPerSecondPerUserPolicy)]
[Route("api/[controller]/[action]")]
public class LogsController(
    ILogger<LogsController> logger,
    IOptions<LoggingOptions> iLoggingOptions,
    ICoreBus bus,
    IOptions<EmailAddressesOptions> iEmailAddressesOptions
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

        await bus.Send(new SendEmailCommand(
            iEmailAddressesOptions.Value.FrontendErrors,
            $"{safeMessage[..Math.Min(80, safeMessage.Length)]}",
            HtmlMessage: $"""
                          RequestId: {HttpContext.TraceIdentifier}<br/>
                          TimeStamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC<br/>
                          Message: {safeMessage}
                          """
        ));

        return Ok();
    }
}