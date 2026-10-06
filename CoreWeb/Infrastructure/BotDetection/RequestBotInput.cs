using System.Net;

namespace CoreWeb.Infrastructure.BotDetection;

public record RequestBotInput(string? UserAgent, IPAddress? ClientIp);
