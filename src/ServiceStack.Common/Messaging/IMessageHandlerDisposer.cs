namespace ServiceStack.Messaging
{
    public interface IMessageHandlerDisposer
    {
        void DisposeMessageHandler(IMessageHandler messageHandler);
    }
}