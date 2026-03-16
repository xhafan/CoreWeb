using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace MrWatchdog.Web.Infrastructure;

// inspired by https://www.learnrazorpages.com/advanced/custom-route-conventions#creating-a-new-convention
public class PageRouteModelConvention : IPageRouteModelConvention
{
    public void Apply(PageRouteModel model)
    {
        foreach (var selector in model.Selectors.ToList())
        {
            if (selector.AttributeRouteModel?.Template == null) continue;

            var templateSplit = selector.AttributeRouteModel.Template.Split("/").ToList();
            if (templateSplit.Count >= 3 && templateSplit[1] == templateSplit[2])
            {
                templateSplit.RemoveAt(1);
            }
            if (templateSplit.Count >= 4 && templateSplit[2] == templateSplit[3])
            {
                templateSplit.RemoveAt(2);
            }
            selector.AttributeRouteModel.Template = string.Join("/", templateSplit);
        }
    }
}