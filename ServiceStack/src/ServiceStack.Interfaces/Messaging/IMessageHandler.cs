using System;

namespace ServiceStack.Messaging
{
    /// <summary>
    /// Single threaded message handler that can process all messages
    /// of a particular message type.
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// The type of the message this handler processes
        /// </summary>
        Type MessageType { get; }

        /// <summary>
        /// The MqClient processing the message
        /// </summary>
        IMessageQueueClient MqClient { get; }

        /// <summary>
        /// Process all messages pending
        /// </summary>
        /// <param name="mqClient"></param>
        void Process(IMessageQueueClient mqClient);

        /// <summary>
        /// Process messages from a single queue.
        /// </summary>
        /// <param name="mqClient"></param>
        /// <param name="queueName">The queue to process</param>
        /// <param name="doNext">A predicate on whether to continue processing the next message if any</param>
        /// <returns></returns>
        int ProcessQueue(IMessageQueueClient mqClient, string queueName, Func<bool> doNext = null);

        /// <summary>
        /// Process a single message
        /// </summary>
        void ProcessMessage(IMessageQueueClient mqClient, object mqResponse);

        /// <summary>
        /// Get Current Stats for this Message Handler
        /// </summary>
        /// <returns></returns>
        IMessageHandlerStats GetStats();
    }
}