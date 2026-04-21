using Reinforced.Typings.Attributes;

namespace CoreWeb.Account.Features.LoginLink.Login;

[TsInterface(IncludeNamespace = false, AutoI = false)]
// ReSharper disable once NotAccessedPositionalProperty.Global - ReCaptchaSiteKey parameter only used by Reinforced.Typings
public record LoginStimulusModel(string ReCaptchaSiteKey);