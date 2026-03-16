using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MrWatchdog.Web.Features.Shared.TagHelpers.ViewOrEditForm;

[HtmlTargetElement("view-mode-item")]
public class ViewModeItem : TagHelper
{
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.Attributes.SetAttribute("data-view-or-edit-form-target", "viewModeItem");
    }
}