using System;

namespace ServiceStack.Messaging.ActiveMq.Support.Converters
{
    public class DestinationTypeConverter
    {
        public static DestinationType Parse(NMS.DestinationType destinationType)
        {
            switch (destinationType)
            {
                case NMS.DestinationType.Queue:
                case NMS.DestinationType.TemporaryQueue:
                    return DestinationType.Queue;

                case NMS.DestinationType.TemporaryTopic:
                case NMS.DestinationType.Topic:
                    return DestinationType.Topic;

                default:
                    throw new NotSupportedException(destinationType.ToString());
            }
        }

        public static NMS.DestinationType ToNmsDestinationType(DestinationType destinationType)
        {
            switch (destinationType)
            {
                case DestinationType.Queue:
                    return NMS.DestinationType.Queue;

                case DestinationType.Topic:
                    return NMS.DestinationType.Topic;

                default:
                    throw new NotSupportedException(destinationType.ToString());
            }
        }

    }
}
