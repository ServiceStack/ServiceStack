#if !SILVERLIGHT 
using System;
using System.Collections;

namespace ServiceStack.Messaging
{
    public class InMemoryTransientMessageService
        : TransientMessageServiceBase
    {
        internal InMemoryTransientMessageFactory Factory { get; set; }

        public InMemoryTransientMessageService()
            : this(null)
        {
        }

        public InMemoryTransientMessageService(InMemoryTransientMessageFactory factory)
        {
            this.Factory = factory ?? new InMemoryTransientMessageFactory(this);
            this.Factory.MqFactory.MessageReceived += factory_MessageReceived;
        }

        void factory_MessageReceived(object sender, EventArgs e)
        {
            //var Factory = (MessageQueueClientFactory) sender;
            this.Start();
        }

        public override IMessageFactory MessageFactory
        {
            get { return Factory; }
        }

        public MessageQueueClientFactory MessageQueueFactory
        {
            get { return Factory.MqFactory; }
        }
    }
}
#endif