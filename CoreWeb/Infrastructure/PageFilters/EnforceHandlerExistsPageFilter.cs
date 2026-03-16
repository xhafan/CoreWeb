using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MrWatchdog.Web.Infrastructure.PageFilters;

public class EnforceHandlerExistsPageFilter : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
    {
        return Task.CompletedTask;
    }

    public Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (context.HandlerMethod == null &&
            context.HttpContext.Request.Query.ContainsKey("handler"))
        {
            context.Result = new NotFoundResult();
            return Task.CompletedTask;
        }

        return next();
    }
}