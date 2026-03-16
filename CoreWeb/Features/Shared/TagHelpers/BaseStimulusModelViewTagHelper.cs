using CoreBackend.Infrastructure.Jsons;
using CoreUtils;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace CoreWeb.Features.Shared.TagHelpers;

public abstract class BaseStimulusModelViewTagHelper<TStimulusModel>(IHtmlHelper htmlHelper) 
    : BaseViewTagHelper(htmlHelper)
{
    protected abstract Task<TStimulusModel> GetStimulusModel();
    
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        await base.ProcessAsync(context, output);

        var stimulusModel = await GetStimulusModel();
        
        var stimulusControllerName = GetStimulusControllerName();
        Guard.Hope(stimulusControllerName != null, $"{GetType().Name} Stimulus controller name not provided.");
        output.Attributes.SetAttribute($"data-{stimulusControllerName}-model-value", JsonHelper.Serialize(stimulusModel));
    }
}