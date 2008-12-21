using System;
using ServiceStack.Messaging.ActiveMq.Support.Wrappers;

namespace ServiceStack.Messaging.ActiveMq
{
    /// <summary>
    /// Accepts a Uri that contains both the Host and Name, e.g.
    /// tcp://localhost:61616/QueueOrTopicName
    /// </summary>
    public class DestinationUri
    {
        private const char QUEUE_NAME_SEPERATOR = '/';
        private string host;
        private string name;

        /// <summary>
        /// A Uri in the format [protocol]://[host]/[queueName] e.g.
        /// tcp://localhost:61616/Name
        /// </summary>
        public DestinationUri(string destinationUri)
        {
            Load(destinationUri);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DestinationUri"/> class.
        /// </summary>
        /// <param name="host">The hostname.</param>
        /// <param name="name">The queue or topic name.</param>
        public DestinationUri(string host, string name)
        {
            this.host = host;
            this.name = name;
        }

        private void Load(string destinationUri)
        {
            if (string.IsNullOrEmpty(destinationUri) || destinationUri.LastIndexOf(QUEUE_NAME_SEPERATOR) == -1)
            {
                throw new ArgumentException(
                    string.Format("Uri '{0}' is not a valid queue destination", destinationUri));
            }
            try
            {
                int queueNameStartIndex = destinationUri.LastIndexOf(QUEUE_NAME_SEPERATOR);
                host = destinationUri.Substring(0, queueNameStartIndex);
                name = destinationUri.Substring(queueNameStartIndex + 1);
            }
            catch(Exception ex)
            {
                throw new ArgumentException(
                    string.Format("Uri '{0}' is not a valid queue destination", destinationUri), ex);
            }
        }

        /// <summary>
        /// A Uri in the format [protocol]://[host]/[queuename] e.g.
        /// tcp://localhost:61616/Name
        /// </summary>
        public static DestinationUri Parse(string queueUri)
        {
            return new DestinationUri(queueUri);
        }

        /// <summary>
        /// A Uri in the format [protocol]://[host]/[queuename] e.g.
        /// tcp://localhost:61616/Name
        /// </summary>
        public string Uri
        {
            get { return host + QUEUE_NAME_SEPERATOR + name; }
            set { Load(value); }
        }

        /// <summary>
        /// Gets or sets the hostname.
        /// </summary>
        /// <value>The hostname.</value>
        public string Host
        {
            get { return host; }
            set { host = value; }
        }

        /// <summary>
        /// Gets or sets the queue or topic name.
        /// </summary>
        /// <value>The queue or topic name.</value>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public override string ToString()
        {
            return Uri;
        }

    }
}