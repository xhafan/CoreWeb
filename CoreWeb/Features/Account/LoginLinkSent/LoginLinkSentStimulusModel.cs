using Reinforced.Typings.Attributes;

namespace MrWatchdog.Web.Features.Account.LoginLinkSent;

[TsInterface(IncludeNamespace = false, AutoI = false)]
// ReSharper disable once NotAccessedPositionalProperty.Global - LoginTokenGuid parameter only used by Reinforced.Typings
public record LoginLinkSentStimulusModel(Guid LoginTokenGuid);