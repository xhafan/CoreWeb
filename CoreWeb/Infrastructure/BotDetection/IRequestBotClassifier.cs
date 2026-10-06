namespace CoreWeb.Infrastructure.BotDetection;

public interface IRequestBotClassifier
{
    RequestBotClassification Classify(RequestBotInput input);
}
