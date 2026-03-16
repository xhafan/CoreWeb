using Microsoft.AspNetCore.Mvc.Rendering;

namespace CoreWeb.Infrastructure.Extensions.HtmlHelpers;

public static class HtmlHelperExtensions
{
    extension(IHtmlHelper htmlHelper)
    {
        public IEnumerable<SelectListItem> GetEnumSelectListWithDefaultValue<TEnum>(TEnum defaultValue) 
            where TEnum : struct
        {
            var selectList = htmlHelper.GetEnumSelectList<TEnum>().ToList();
            selectList.Single(x => x.Value == $"{(int)(object)defaultValue}").Selected = true;
            return selectList;
        }
    }
}