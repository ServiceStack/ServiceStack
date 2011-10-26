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
		/// Process all messages pending
		/// </summary>
		/// <param name="mqClient"></param>
		void Process(IMessageQueueClient mqClient);

        /// <summary>
        /// Get Current Stats for this Message Handler
        /// </summary>
        /// <returns></returns>
        IMessageHandlerStats GetStats();
	}
}