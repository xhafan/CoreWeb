using CoreUtils;
using MrWatchdog.Core.Infrastructure.Rebus;
using MrWatchdog.Core.Infrastructure.Rebus.RebusQueueRedirectors;

namespace MrWatchdog.Web.Infrastructure.Rebus.RebusQueueRedirectors;

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