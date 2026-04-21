namespace CoreWeb.Account.Features.LoginLink.Login;

public class ReCaptchaOptions
{
    public string SiteKey { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
}