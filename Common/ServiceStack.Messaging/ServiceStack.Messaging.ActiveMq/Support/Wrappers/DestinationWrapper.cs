using System;
using System.Text;
using ServiceStack.Messaging.ActiveMq.Support.Converters;

namespace ServiceStack.Messaging.ActiveMq.Support.Wrappers
{
    public class DestinationWrapper : INmsDestination
    {
        private readonly string brokerUri;
        private readonly NMS.IDestination destination;

        public DestinationWrapper(NMS.IDestination destination, string brokerUri)
        {
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }
            if (string.IsNullOrEmpty(brokerUri))
            {
                throw new ArgumentNullException("brokerUri");
            }
            this.brokerUri = brokerUri;
            this.destination = destination;
        }

        public bool IsTemp
        {
            get
            {
                return destination.DestinationType == NMS.DestinationType.TemporaryQueue ||
                       destination.DestinationType == NMS.DestinationType.TemporaryTopic;
            }
        }

        public DestinationType DestinationType
        {
            get
            {
                return DestinationTypeConverter.Parse(destination.DestinationType);
            }
        }

        public string Uri
        {
            get
            {
                return new DestinationUri(brokerUri, GetNmsDestinationName(destination)).Uri;
            }
        }

        public NMS.IDestination NmsDestination
        {
            get { return destination; }
        }

        private static string GetNmsDestinationName(NMS.IDestination destination)
        {
            ActiveMQ.Commands.ActiveMQDestination amqDestination =
                destination as ActiveMQ.Commands.ActiveMQDestination;
            if (amqDestination != null)
            {
                return amqDestination.PhysicalName;
            }
            throw new System.NotSupportedException("No Uri found for type: " + destination.GetType().FullName);
        }

        public override string ToString()
        {
            return string.Format("[type='{0}' uri='{1}]", DestinationType, Uri);
        }
    }
}