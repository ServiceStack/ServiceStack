using System;
using System.Collections;

namespace ServiceStack.Messaging
{
	public class InMemoryMessagingService 
		: MessagingServiceBase
	{
		private readonly MessageQueueClientFactory factory;

		public InMemoryMessagingService()
			: this(null)
		{
		}

		public InMemoryMessagingService(MessageQueueClientFactory factory)
		{
			this.factory = factory ?? new MessageQueueClientFactory();
			this.factory.MessageReceived += factory_MessageReceived;
		}

		void factory_MessageReceived(object sender, EventArgs e)
		{
			//var factory = (MessageQueueClientFactory) sender;
			this.Start();
		}

		public override IMessageQueueClientFactory MessageFactory
		{
			get { return factory; }
		}
	}
}