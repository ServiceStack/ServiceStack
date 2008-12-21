using System;
using ServiceStack.Messaging.ActiveMq;

namespace ServiceStack.Messaging.ActiveMq
{
    /// <summary>
    /// Helper class to listen for messages on the specified queue.
    /// Messages received can be processed by attaching to the MessageReceived event.
    /// </summary>
    public class ActiveMqQueueListener : ActiveMqListenerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveMqQueueListener"/> class.
        /// </summary>
        /// <param name="nmsConnectionManager">The NMS connection manager.</param>
        /// <param name="destination">The destination.</param>
        public ActiveMqQueueListener(INmsConnectionManager nmsConnectionManager, IDestination destination)
            : base(nmsConnectionManager, destination) { }

        /// <summary>
        /// Gets the type of the destination. e.g. topic or queue.
        /// </summary>
        /// <value>The type of the destination.</value>
        public override DestinationType DestinationType
        {
            get { return DestinationType.Queue; }
        }

        /// <summary>
        /// Starts this instance listening to the queue provided.
        /// </summary>
        protected override void OnConnect()
        {
            base.OnConnect();
            RegisterConsumer(Session.GetQueue(DestinationUri.Name));
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public override void Start()
        {
            OnConnect();
        }
    }
}