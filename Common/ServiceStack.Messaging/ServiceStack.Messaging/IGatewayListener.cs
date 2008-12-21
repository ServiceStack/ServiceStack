namespace ServiceStack.Messaging
{
    /// <summary>
    /// An Messaging listener used to receive message from a queue or topic
    /// </summary>
    public interface IGatewayListener : IResource
    {

        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        event MessageReceivedHandler MessageReceived;

        /// <summary>
        /// Gets or sets the connection id.
        /// </summary>
        /// <value>The connection id.</value>
        string ConnectionId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the dead letter queue.
        /// Poision messages will be sent to this queue.
        /// </summary>
        /// <value>The dead letter queue.</value>
        string DeadLetterQueue
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the maximum redelivery count for failed messages.
        /// messages that fail after this no will be sent to the dead letter queue.
        /// </summary>
        /// <value>The maximum redelivery count.</value>
        int MaximumRedeliveryCount
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the destination.
        /// </summary>
        /// <value>The destination.</value>
        IDestination Destination
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the failover settings.
        /// </summary>
        /// <value>The failover settings.</value>
        FailoverSettings FailoverSettings
        {
            get;
        }

        /// <summary>
        /// Commits this instance.
        /// </summary>
        void Commit();

        /// <summary>
        /// Rollbacks this instance.
        /// </summary>
        void Rollback();

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <value>The connection.</value>
        IConnection Connection
        { 
            get;
        }
    }
}
