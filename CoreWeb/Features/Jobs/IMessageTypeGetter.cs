namespace CoreWeb.Features.Jobs;

public interface IMessageTypeGetter
{
    Type GetMessageType(string messageTypeName);
}