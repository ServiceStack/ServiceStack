namespace ServiceStack.Messaging
{
    /// <summary>
    /// Encapsulates creating a new message handler
    /// </summary>
    public interface IMessageHandlerFactory
    {
        IMessageHandler CreateMessageHandler();
    }
}