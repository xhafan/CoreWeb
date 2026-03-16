using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MrWatchdog.Web.Features.Shared.TagHelpers.ViewOrEditForm;

[HtmlTargetElement("view-or-edit-form")]
public class ViewOrEditForm(IHtmlHelper htmlHelper) 
    : BaseStimulusModelViewTagHelper<ViewOrEditFormStimulusModel>(htmlHelper)
{
    public string? Action { get; set; }
    public bool StartInEditMode { get; set; }
    public bool HideCancelInEditMode { get; set; }
    
    protected override string GetStimulusControllerName()
    {
        return StimulusControllers.ViewOrEditForm;
    }

    protected override Task<ViewOrEditFormStimulusModel> GetStimulusModel()
    {
        return Task.FromResult(new ViewOrEditFormStimulusModel(StartInEditMode, HideCancelInEditMode));
    }
}