using System.Diagnostics;
using CoreBackend.Infrastructure.ErrorReporting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CoreWeb.Infrastructure.Middlewares;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private const string BotClassificationBot = "Bot";
    private const string BotClassificationNotBot = "NotBot";
    private const string BotClassificationUnknown = "Unknown";

    private static readonly string[] BotMarkers =
    [
        "bot",
        "crawler",
        "spider",
        "slurp",
        "facebookexternalhit",
        "curl/",
        "wget/",
        "python-requests",
        "go-http-client",
        "headlesschrome",
        "postmanruntime",
        "axios/",
        "okhttp/"
    ];

    private static readonly string[] HeadersToLog =
    [
        "User-Agent",
        "Accept-Language",
        "Referer",
        "Purpose",
        "X-Sec-Purpose",
        "Turbo-Frame",
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
            var endpointName = context.GetEndpoint()?.DisplayName ?? "Unknown";
            var isInternalFrame = context.GetEndpoint()?.Metadata
                .GetMetadata<IInternalFrameEndpointMetadata>()?.IsInternalFrame == true;
            var turboFrame = request.Headers["Turbo-Frame"].ToString();
            var botClassification = _classifyBot(request.Headers.UserAgent.ToString());

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

            var isPageRequest = HttpMethods.IsGet(request.Method)
                                && !isInternalFrame
                                && string.IsNullOrEmpty(turboFrame)
                                && context.Response.ContentType?.StartsWith(
                                    "text/html",
                                    StringComparison.OrdinalIgnoreCase
                                ) == true;

            logger.LogInformation(
                "Request completed: {Method} {Path} in {Elapsed} ms with status {StatusCode}; Endpoint={EndpointName}; IsPageRequest={IsPageRequest}; TurboFrame={TurboFrame}; BotClassification={BotClassification}",
                request.Method,
                request.Path + request.QueryString,
                stopwatch.ElapsedMilliseconds,
                context.Response.StatusCode,
                endpointName,
                isPageRequest,
                turboFrame,
                botClassification
            );
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            stopwatch.Stop();
            var request = context.Request;

            logger.LogInformation(
                "Request canceled by the client: {Method} {Path} after {Elapsed} ms",
                request.Method,
                request.Path + request.QueryString,
                stopwatch.ElapsedMilliseconds
            );
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            using (ErrorReportingLogPolicy.Suppress())
            {
                logger.LogError(
                    ex,
                    "Request failed after {Elapsed} ms",
                    stopwatch.ElapsedMilliseconds
                );
            }

            throw;
        }
    }

    private static string _classifyBot(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return BotClassificationUnknown;
        }

        return BotMarkers.Any(marker => userAgent.Contains(marker, StringComparison.OrdinalIgnoreCase))
            ? BotClassificationBot
            : BotClassificationNotBot;
    }
}
