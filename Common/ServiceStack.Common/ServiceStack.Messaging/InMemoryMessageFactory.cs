using System;
using ServiceStack.Logging;

namespace ServiceStack.Messaging
{
	public class InMemoryMessageFactory
		: IMessageFactory
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(InMemoryMessageFactory));
		private readonly InMemoryMessagingService  messagingService;

		public InMemoryMessageFactory()
			: this(null)
		{
		}

		public InMemoryMessageFactory(InMemoryMessagingService messagingService)
		{
			this.messagingService = messagingService ?? new InMemoryMessagingService();
		}

		public IMessageProducer CreateMessageProducer()
		{
			return new InMemoryMessageProducer(this);
		}

		public IMessageService CreateMessageService()
		{
			return messagingService;
		}

		public void Dispose()
		{
			Log.DebugFormat("Disposing InMemoryMessageFactory...");
		}


		internal class InMemoryMessageProducer
			: IMessageProducer
		{
			private readonly InMemoryMessageFactory parent;

			public InMemoryMessageProducer(InMemoryMessageFactory parent)
			{
				this.parent = parent;
			}

			public void Publish<T>(T messageBody)
			{
				Publish((IMessage<T>)new Message<T>(messageBody));
			}

			public void Publish<T>(IMessage<T> message)
			{
				this.parent.messagingService.Factory.PublishMessage(QueueNames<T>.In, message);
			}

			public void Dispose()
			{
				Log.DebugFormat("Disposing InMemoryMessageProducer...");
			}
		}

	}
}