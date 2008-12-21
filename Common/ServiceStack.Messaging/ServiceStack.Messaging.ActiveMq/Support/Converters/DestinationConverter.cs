using System;
using ServiceStack.Logging;

namespace ServiceStack.Messaging.ActiveMq.Support.Converters
{
    /// <summary>
    /// IDestination / NMS.IDestination Converter
    /// </summary>
    public class DestinationConverter
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DestinationConverter));

        /// <summary>
        /// Parses the specified destination.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <returns></returns>
        public static IDestination Parse(NMS.IDestination destination)
        {
            ActiveMQ.Commands.ActiveMQDestination amqDestination = 
                destination as ActiveMQ.Commands.ActiveMQDestination;
            if (amqDestination != null)
            {
                DestinationType destinationType = DestinationTypeConverter.Parse(amqDestination.DestinationType);
                return new Destination(destinationType, amqDestination.PhysicalName);
            }

            throw new NotSupportedException(destination.ToString());
        }

        /// <summary>
        /// Convert to the NMS destination.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="destination">The destination.</param>
        /// <returns></returns>
        public static NMS.IDestination ToNmsDestination(NMS.ISession session, IDestination destination)
        {
            INmsDestination nmsDestination = destination as INmsDestination;
            if (nmsDestination != null)
            {
                log.DebugFormat("Converting NMS {0} destination {1}", destination.DestinationType, destination.Uri);
                return nmsDestination.NmsDestination;
            }
            log.DebugFormat("Converting NMS {0} destination {1}", destination.DestinationType, destination.Uri);
            switch (destination.DestinationType)
            {
                case DestinationType.Queue:
                    return session.GetQueue(new DestinationUri(destination.Uri).Name);

                case DestinationType.Topic:
                    return session.GetTopic(new DestinationUri(destination.Uri).Name);
            }

            throw new NotSupportedException(destination.DestinationType.ToString());
        }
    }
}