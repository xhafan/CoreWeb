using System.Net;

namespace MrWatchdog.Web.Infrastructure.Middlewares;

// During deployment, a page can be loaded from an old app version, and assets from a new version.
// This middleware handles requests for old fingerprinted assets (e.g. /assets/bundle.abc123.js)
// and redirects to the non-fingerprinted version (e.g. /assets/bundle.js).
public class HandleOldAssetRequestsMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext httpContext)
    {
        await next(httpContext); // let other middleware/endpoints try to handle the request

        var path = httpContext.Request.Path.Value ?? "";

        if (httpContext.Response.StatusCode == (int)HttpStatusCode.NotFound
            && path.StartsWith("/assets/")
            && path.Contains("bundle.") 
            && (path.EndsWith(".js") || path.EndsWith(".css")))
        {
            httpContext.Response.Clear();

            // Redirect to the non-fingerprinted version
            var fallbackPath = path.EndsWith(".js")
                ? "/assets/bundle.js"
                : "/assets/bundle.css";
            httpContext.Response.Redirect(fallbackPath);
        }
    }
}