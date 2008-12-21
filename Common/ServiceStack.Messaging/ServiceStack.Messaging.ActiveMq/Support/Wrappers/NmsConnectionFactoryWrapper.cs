using NMS;

namespace ServiceStack.Messaging.ActiveMq.Support.Wrappers
{
    public class NmsConnectionFactoryWrapper : INmsConnectionFactory
    {
        private readonly string connectionString;

        public NmsConnectionFactoryWrapper(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public NMS.IConnection CreateNmsConnection()
        {
            return new ActiveMQ.ConnectionFactory(connectionString).CreateConnection();
        }
    }
}