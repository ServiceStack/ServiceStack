using System;
using System.Collections;

namespace ServiceStack.Messaging
{
	public class InMemoryTransientMessageService
		: TransientMessageServiceBase
	{
		internal MessageQueueClientFactory Factory { get; set; }

		public InMemoryTransientMessageService()
			: this(null)
		{
		}

		public InMemoryTransientMessageService(MessageQueueClientFactory factory)
		{
			this.Factory = factory ?? new MessageQueueClientFactory();
			this.Factory.MessageReceived += factory_MessageReceived;
		}

		void factory_MessageReceived(object sender, EventArgs e)
		{
			//var Factory = (MessageQueueClientFactory) sender;
			this.Start();
		}

		public override IMessageQueueClientFactory MessageFactory
		{
			get { return Factory; }
		}
	}
}