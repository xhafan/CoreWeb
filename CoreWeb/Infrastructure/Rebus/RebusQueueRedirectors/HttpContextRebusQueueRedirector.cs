using CoreBackend.Infrastructure.Rebus;
using CoreBackend.Infrastructure.Rebus.RebusQueueRedirectors;
using CoreUtils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace CoreWeb.Infrastructure.Rebus.RebusQueueRedirectors;

public class HttpContextRebusQueueRedirector(
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment webHostEnvironment
) : IRebusQueueRedirector
{
    public string? GetQueueForRedirection()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null) return null;

        if (!httpContext.Request.Headers.TryGetValue(CustomHeaders.QueueForRedirection, out var queueForRedirectionHeaderValue))
        {
            return null;
        }

        var queueForRedirection = queueForRedirectionHeaderValue.ToString();
        Guard.Hope(RebusQueues.AllQueues.Value.Contains(queueForRedirection), $"Unsupported Rebus queue {queueForRedirection}.");

        return $"{webHostEnvironment.EnvironmentName}{queueForRedirection}";
    }
}