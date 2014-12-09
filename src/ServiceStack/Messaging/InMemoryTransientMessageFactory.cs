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
        private readonly InMemoryTransientMessageService  transientMessageService;
        internal MessageQueueClientFactory MqFactory { get; set; }

        public InMemoryTransientMessageFactory()
            : this(null)
        {
        }

        public InMemoryTransientMessageFactory(InMemoryTransientMessageService transientMessageService)
        {
            this.transientMessageService = transientMessageService ?? new InMemoryTransientMessageService();
            this.MqFactory = new MessageQueueClientFactory();
        }

        public IMessageProducer CreateMessageProducer()
        {
            return new InMemoryMessageProducer(this);
        }

        public IMessageQueueClient CreateMessageQueueClient()
        {
            return new InMemoryMessageQueueClient(MqFactory);
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
                var message = messageBody as IMessage;
                if (message != null)
                {
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
                    .PublishMessage(queueName, message.ToBytes());
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