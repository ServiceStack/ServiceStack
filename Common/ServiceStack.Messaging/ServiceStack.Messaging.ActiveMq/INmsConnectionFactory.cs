namespace ServiceStack.Messaging.ActiveMq
{
    public interface INmsConnectionFactory
    {
        NMS.IConnection CreateNmsConnection();
    }
}