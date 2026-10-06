namespace CoreWeb.Infrastructure.BotDetection;

public record RequestBotClassification(string Classification, string BotName, string Reason, string RulesVersion);
