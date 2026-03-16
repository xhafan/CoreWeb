using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MrWatchdog.Web.Features.Shared.TagHelpers.TurboFrame;

[HtmlTargetElement("turbo-frame")]
public class TurboFrame : TagHelper
{
    public string? ReloadOnEvent { get; set; }
    
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        await base.ProcessAsync(context, output);

        output.Attributes.SetAttribute("data-controller", StimulusControllers.TurboFrame);
        if (!string.IsNullOrWhiteSpace(ReloadOnEvent))
        {
            output.Attributes.SetAttribute($"data-{StimulusControllers.TurboFrame}-reload-on-event-value", ReloadOnEvent);
        }
        output.TagMode = TagMode.StartTagAndEndTag;
    }
}