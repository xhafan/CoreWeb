using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using CoreWeb.Infrastructure.BotDetection;
using Microsoft.Extensions.Logging;

namespace CoreWeb.Infrastructure.Middlewares;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger, IRequestBotClassifier botClassifier)
{
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
            var isInternalFragment = context.GetEndpoint()?.Metadata
                .GetMetadata<IInternalFragmentEndpointMetadata>()?.IsInternalFragment == true;
            var turboFrame = request.Headers["Turbo-Frame"].ToString();
            var botClassification = botClassifier.Classify(new RequestBotInput(request.Headers.UserAgent.ToString(), connection.RemoteIpAddress));

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
                                && !isInternalFragment
                                && string.IsNullOrEmpty(turboFrame)
                                && context.Response.ContentType?.StartsWith(
                                    "text/html",
                                    StringComparison.OrdinalIgnoreCase
                                ) == true;

            logger.LogInformation(
                "Request completed: {Method} {Path} in {Elapsed} ms with status {StatusCode}; Endpoint={EndpointName}; IsPageRequest={IsPageRequest}; TurboFrame={TurboFrame}; BotClassification={BotClassification}; BotName={BotName}; BotClassificationReason={BotClassificationReason}; BotRulesVersion={BotRulesVersion}",
                request.Method,
                request.Path + request.QueryString,
                stopwatch.ElapsedMilliseconds,
                context.Response.StatusCode,
                endpointName,
                isPageRequest,
                turboFrame,
                botClassification.Classification,
                botClassification.BotName,
                botClassification.Reason,
                botClassification.RulesVersion
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

            logger.LogError(
                ex,
                "Request failed after {Elapsed} ms",
                stopwatch.ElapsedMilliseconds
            );

            throw;
        }
    }

}
