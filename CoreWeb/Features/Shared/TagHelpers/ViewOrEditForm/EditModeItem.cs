using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MrWatchdog.Web.Features.Shared.TagHelpers.ViewOrEditForm;

[HtmlTargetElement("edit-mode-item")]
public class EditModeItem : TagHelper
{
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.Attributes.SetAttribute("style", "display: none");
        output.Attributes.SetAttribute("data-view-or-edit-form-target", "editModeItem");
    }
}