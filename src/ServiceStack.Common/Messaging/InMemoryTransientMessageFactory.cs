using System;
using ServiceStack.Logging;

namespace ServiceStack.Messaging
{
	public class InMemoryTransientMessageFactory
		: IMessageFactory
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(InMemoryTransientMessageFactory));
		private readonly InMemoryTransientMessageService  transientMessageService;

		public InMemoryTransientMessageFactory()
			: this(null)
		{
		}

		public InMemoryTransientMessageFactory(InMemoryTransientMessageService transientMessageService)
		{
			this.transientMessageService = transientMessageService ?? new InMemoryTransientMessageService();
		}

		public IMessageProducer CreateMessageProducer()
		{
			return new InMemoryMessageProducer(this);
		}

		public IMessageService CreateMessageService()
		{
			return transientMessageService;
		}

		public void Dispose()
		{
			Log.DebugFormat("Disposing InMemoryTransientMessageFactory...");
		}


		internal class InMemoryMessageProducer
			: IMessageProducer
		{
			private readonly InMemoryTransientMessageFactory parent;

			public InMemoryMessageProducer(InMemoryTransientMessageFactory parent)
			{
				this.parent = parent;
			}

			public void Publish<T>(T messageBody)
			{
				Publish((IMessage<T>)new Message<T>(messageBody));
			}

			public void Publish<T>(IMessage<T> message)
			{
				this.parent.transientMessageService.Factory.PublishMessage(QueueNames<T>.In, message);
			}

			public void Dispose()
			{
				Log.DebugFormat("Disposing InMemoryMessageProducer...");
			}
		}

	}
}