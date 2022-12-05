#if !SL5 
using System;

namespace ServiceStack.Messaging
{
    public class InMemoryTransientMessageService
        : TransientMessageServiceBase
    {
        internal InMemoryTransientMessageFactory Factory { get; set; }

        public InMemoryTransientMessageService()
            : this(factory: null)
        {
        }

        public InMemoryTransientMessageService(InMemoryTransientMessageFactory factory)
            : this(factory, new ServiceStackTextMessageByteSerializer())
        {
        }

        public InMemoryTransientMessageService(IMessageByteSerializer messageByteSerializer)
            : this(null, messageByteSerializer)
        {
        }

        public InMemoryTransientMessageService(InMemoryTransientMessageFactory factory, IMessageByteSerializer messageByteSerializer)
        {
            this.Factory = factory ?? new InMemoryTransientMessageFactory(this, messageByteSerializer);
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
