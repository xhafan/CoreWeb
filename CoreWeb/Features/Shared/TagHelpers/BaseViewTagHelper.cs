using CoreUtils;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MrWatchdog.Web.Features.Shared.TagHelpers;

public abstract class BaseViewTagHelper(IHtmlHelper htmlHelper) : TagHelper 
{
    protected virtual string? TagName => "div";
    
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; } = null!;
    
    public TagHelperContent ChildContent { get; private set; } = null!;
    
    protected virtual string? GetStimulusControllerName()
    {
        return null;
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (htmlHelper is IViewContextAware viewContextAware)
        {
            viewContextAware.Contextualize(ViewContext);
        }
        
        var type = GetType();
        Guard.Hope(type.Namespace != null, $"{type.Name} namespace is null.");
        var assemblyName = type.Assembly.GetName().Name;
        var viewLocation = $"/{type.Namespace.Replace($"{assemblyName}.", "").Replace(".", "/")}/{type.Name}.cshtml";
        
        ChildContent = await output.GetChildContentAsync();

        var partial = await htmlHelper.PartialAsync(viewLocation, this, null);

        output.TagName = TagName;
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Content.SetHtmlContent(partial);
        
        var stimulusControllerName = GetStimulusControllerName();

        if (stimulusControllerName != null)
        {
            output.Attributes.SetAttribute("data-controller", stimulusControllerName);        
        }
    }
}