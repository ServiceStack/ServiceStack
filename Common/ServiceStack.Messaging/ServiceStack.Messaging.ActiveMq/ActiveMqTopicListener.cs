using System;

namespace ServiceStack.Messaging.ActiveMq
{
    /// <summary>
    /// Helper class to listen for messages on the specified topic.
    /// Messages received can be processed by attaching to the MessageReceived event.
    /// </summary>
    public class ActiveMqTopicListener : ActiveMqListenerBase, IRegisteredListener
    {
        private string durableSubscriberId;
        private const string CLIENT_ID_SUFFIX = "ClientId";

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveMqTopicListener"/> class.
        /// </summary>
        /// <param name="nmsConnectionManager">The NMS connection manager.</param>
        /// <param name="destination">The destination.</param>
        public ActiveMqTopicListener(INmsConnectionManager nmsConnectionManager, IDestination destination)
            : base(nmsConnectionManager, destination) { }

        /// <summary>
        /// Gets or sets the durable subscriber id.
        /// </summary>
        /// <value>The durable subscriber id.</value>
        public string SubscriberId
        {
            get { return durableSubscriberId; }
            set 
            { 
                durableSubscriberId = value;
                if (string.IsNullOrEmpty(durableSubscriberId))
                {
                    ConnectionId = null;
                }
                else
                {
                    ConnectionId = durableSubscriberId + CLIENT_ID_SUFFIX;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is durable subscriber.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is durable subscriber; otherwise, <c>false</c>.
        /// </value>
        private bool IsDurableSubscriber
        {
            get { return !string.IsNullOrEmpty(durableSubscriberId); }
        }

        /// <summary>
        /// Gets the type of the destination. e.g. topic or queue.
        /// </summary>
        /// <value>The type of the destination.</value>
        public override DestinationType DestinationType
        {
            get { return DestinationType.Topic; }
        }

        /// <summary>
        /// Starts this instance listening to the topic provided.
        /// </summary>
        protected override void OnConnect()
        {
            base.OnConnect();
            if (IsDurableSubscriber)
            {
                RegisterDurableConsumer(Session.GetTopic(DestinationUri.Name), SubscriberId);
            }
            else
            {
                RegisterConsumer(Session.GetTopic(DestinationUri.Name));
            }
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