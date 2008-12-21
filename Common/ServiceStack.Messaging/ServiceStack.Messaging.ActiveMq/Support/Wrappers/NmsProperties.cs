using System;

namespace ServiceStack.Messaging.ActiveMq.Support.Wrappers
{
    public class NmsProperties
    {
        public static string DeliveryCount { get { return "NMSXDeliveryCount"; } }
        public static string SessionId { get { return "JMSXGroupID"; } }
    }
}