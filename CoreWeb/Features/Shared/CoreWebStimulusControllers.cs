using Reinforced.Typings.Attributes;

namespace CoreWeb.Features.Shared;

[TsClass(IncludeNamespace = false)]
public static class CoreWebStimulusControllers
{
    [TsProperty(Constant = true, ShouldBeCamelCased = true)]
    public const string TurboFrame = "turbo-frame";

    [TsProperty(Constant = true, ShouldBeCamelCased = true)]
    public const string ViewOrEditForm = "view-or-edit-form";
}