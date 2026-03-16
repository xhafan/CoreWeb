using Reinforced.Typings.Attributes;

namespace MrWatchdog.Web.Features.Shared.TagHelpers.ViewOrEditForm;

[TsInterface(IncludeNamespace = false, AutoI = false)]
public record ViewOrEditFormStimulusModel(
    // ReSharper disable NotAccessedPositionalProperty.Global
    bool StartInEditMode,
    bool HideCancelInEditMode
    // ReSharper restore NotAccessedPositionalProperty.Global
);
