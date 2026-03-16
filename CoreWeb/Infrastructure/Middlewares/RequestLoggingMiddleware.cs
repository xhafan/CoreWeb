using System.Diagnostics;

namespace MrWatchdog.Web.Infrastructure.Middlewares;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private static readonly string[] HeadersToLog =
    [
        "User-Agent",
        "Accept-Language",
        "Referer",
        "X-Forwarded-For",
        "Host",
        "Content-Type",
        "Content-Length"
    ];
    
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var request = context.Request;
            var connection = context.Connection;

            var headers = request.Headers
                .Where(h => HeadersToLog.Contains(h.Key, StringComparer.OrdinalIgnoreCase))
                .Select(h => $"{h.Key}={h.Value}")
                .ToArray();
            
            logger.LogInformation(
                "Request started: {Method} {Path} {Protocol} from {RemoteIp}:{RemotePort}, Headers: {Headers}",
                request.Method,
                request.Path + request.QueryString,
                request.Protocol,
                connection.RemoteIpAddress,
                connection.RemotePort,
                string.Join(", ", headers)
            );

            await next(context);

            stopwatch.Stop();

            logger.LogInformation(
                "Request completed: {Method} {Path} in {Elapsed} ms with status {StatusCode}",
                request.Method,
                request.Path + request.QueryString,
                stopwatch.ElapsedMilliseconds,
                context.Response.StatusCode
            );
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            logger.LogError(
                ex,
                "Request failed after {Elapsed} ms",
                stopwatch.ElapsedMilliseconds
            );

            throw;
        }
    }
}
