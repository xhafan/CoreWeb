using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace CoreWeb.Features.Shared.TagHelpers.Hint;

[HtmlTargetElement("hint")]
public class Hint(IHtmlHelper htmlHelper) : BaseViewTagHelper(htmlHelper)
{
    protected override string TagName => "span";

    public string? Text { get; set; }
    public string? FontAwesomeIcon { get; set; } = "fa-question-circle";

    protected override string GetStimulusControllerName()
    {
        return CoreWebStimulusControllers.Hint;
    }
}