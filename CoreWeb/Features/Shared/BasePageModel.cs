using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MrWatchdog.Web.Features.Shared;

public abstract class BasePageModel : PageModel
{
    // Hotwire Turbo requires status 422 after form submit to render form validation errors, more info https://github.com/hotwired/turbo-rails/issues/12#issuecomment-754629885
    protected PageResult PageWithUnprocessableEntityStatus422()
    {
        var pageResult = Page();
        pageResult.StatusCode = StatusCodes.Status422UnprocessableEntity;
        return pageResult;
    }

    protected IActionResult Ok(object? value = null) => new OkObjectResult(value);
}