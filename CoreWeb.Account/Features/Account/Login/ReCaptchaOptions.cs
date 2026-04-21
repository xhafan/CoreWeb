namespace CoreWeb.Account.Features.Account.Login;

public class ReCaptchaOptions
{
    public string SiteKey { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
}