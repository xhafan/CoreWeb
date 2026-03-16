using System.Reflection;
using CoreBackend.Messages;
using CoreUtils;

namespace CoreWeb.Features.Jobs;

public class MessageTypeGetter : IMessageTypeGetter
{
    private readonly ISet<Type> _messageTypes;

    public MessageTypeGetter(IEnumerable<Assembly> assembliesWithTypesDerivedFromBaseMessage)
    {
        _messageTypes = assembliesWithTypesDerivedFromBaseMessage
            .SelectMany(x => x.GetTypes())
            .Where(x => typeof(BaseMessage).IsAssignableFrom(x)
                        && !x.IsAbstract
                        && x.IsPublic)
            .ToHashSet();
    }

    public Type GetMessageType(string messageTypeName)
    {
        var messageType = _messageTypes.SingleOrDefault(x => x.Name == messageTypeName);

        Guard.Hope(messageType != null, $"There is no message of type {messageTypeName}.");
        return messageType;
    }
}