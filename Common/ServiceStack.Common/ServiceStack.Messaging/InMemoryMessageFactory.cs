using System;
using ServiceStack.Logging;

namespace ServiceStack.Messaging
{
	public class InMemoryMessageFactory
		: IMessageFactory
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(InMemoryMessageFactory));
		private readonly InMemoryMessageService  messageService;

		public InMemoryMessageFactory()
			: this(null)
		{
		}

		public InMemoryMessageFactory(InMemoryMessageService messageService)
		{
			this.messageService = messageService ?? new InMemoryMessageService();
		}

		public IMessageProducer CreateMessageProducer()
		{
			return new InMemoryMessageProducer(this);
		}

		public IMessageService CreateMessageService()
		{
			return messageService;
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
				this.parent.messageService.Factory.PublishMessage(QueueNames<T>.In, message);
			}

			public void Dispose()
			{
				Log.DebugFormat("Disposing InMemoryMessageProducer...");
			}
		}

	}
}