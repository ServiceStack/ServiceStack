#if !SL5 
using System;

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
            this.Start();
        }

        public override IMessageFactory MessageFactory => Factory;

        public MessageQueueClientFactory MessageQueueFactory => Factory.MqFactory;
    }
}
#endif