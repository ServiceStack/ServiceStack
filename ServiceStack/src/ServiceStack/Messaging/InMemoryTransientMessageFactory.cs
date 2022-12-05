#if !SL5
using System;
using System.Collections.Generic;
using ServiceStack.Logging;

namespace ServiceStack.Messaging
{
    public class InMemoryTransientMessageFactory
        : IMessageFactory
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(InMemoryTransientMessageFactory));
        private readonly InMemoryTransientMessageService transientMessageService;
        private readonly IMessageByteSerializer messageByteSerializer;
        internal MessageQueueClientFactory MqFactory { get; set; }

        public InMemoryTransientMessageFactory()
            : this(null)
        {
        }
        
        public InMemoryTransientMessageFactory(InMemoryTransientMessageService transientMessageService)
            : this(transientMessageService, new ServiceStackTextMessageByteSerializer())
        {
        }

        public InMemoryTransientMessageFactory(InMemoryTransientMessageService transientMessageService, IMessageByteSerializer messageByteSerializer)
        {
            this.transientMessageService = transientMessageService ?? new InMemoryTransientMessageService(messageByteSerializer);
            this.messageByteSerializer = messageByteSerializer;
            this.MqFactory = new MessageQueueClientFactory(messageByteSerializer);
        }

        public IMessageProducer CreateMessageProducer()
        {
            return new InMemoryMessageProducer(this);
        }

        public IMessageQueueClient CreateMessageQueueClient()
        {
            return new InMemoryMessageQueueClient(MqFactory, messageByteSerializer);
        }

        public IMessageService CreateMessageService()
        {
            return transientMessageService;
        }

        public void Dispose()
        {
        }

        internal class InMemoryMessageProducer
            : IMessageProducer, IOneWayClient
        {
            private readonly InMemoryTransientMessageFactory parent;

            public InMemoryMessageProducer(InMemoryTransientMessageFactory parent)
            {
                this.parent = parent;
            }

            public void Publish<T>(T messageBody)
            {
                if (messageBody is IMessage message)
                {
                    Diagnostics.ServiceStack.Init(message);
                    Publish(message.ToInQueueName(), message);
                }
                else
                {
                    Publish(new Message<T>(messageBody));
                }
            }

            public void Publish<T>(IMessage<T> message)
            {
                Publish(message.ToInQueueName(), message);
            }

            public void Publish(string queueName, IMessage message)
            {
                this.parent.transientMessageService.MessageQueueFactory
                    .PublishMessage(queueName, this.parent.messageByteSerializer.ToBytes(message));
            }

            public void SendOneWay(object requestDto)
            {
                Publish(MessageFactory.Create(requestDto));
            }

            public void SendOneWay(string queueName, object requestDto)
            {
                Publish(queueName, MessageFactory.Create(requestDto));
            }

            public void SendAllOneWay(IEnumerable<object> requests)
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
            }
        }

    }
}
#endif
