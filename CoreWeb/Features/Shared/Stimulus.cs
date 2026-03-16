using CoreBackend.Infrastructure.Jsons;
using Microsoft.AspNetCore.Html;

namespace CoreWeb.Features.Shared;

public static class Stimulus
{
    public static IHtmlContent Controller(
        string stimulusControllerName,
        object? stimulusModel = null
    )
    {
        var htmlContentBuilder = new HtmlContentBuilder();
        var content = htmlContentBuilder.AppendHtmlLine($"""
                                                         data-controller="{stimulusControllerName}"
                                                         """);
        if (stimulusModel != null)
        {
            content = htmlContentBuilder.AppendHtml($"""
                                                     data-{stimulusControllerName}-model-value="
                                                     """)
                .Append(JsonHelper.Serialize(stimulusModel))
                .AppendHtmlLine("\"");
        }
        return content;
    }
}