using System;
using System.Collections.Generic;
using ServiceStack.Messaging;
using ServiceStack.Text;
#if NETCORE
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
#else
using Microsoft.ServiceBus.Messaging;
#endif

namespace ServiceStack.Azure.Messaging
{
    public class ServiceBusMqMessageProducer : IMessageProducer
    {
        private readonly Dictionary<string, MessageReceiver> sbReceivers = new Dictionary<string, MessageReceiver>();
        protected readonly ServiceBusMqMessageFactory parentFactory;
        
#if NETCORE
        public Action<Microsoft.Azure.ServiceBus.Message,IMessage> PublishMessageFilter { get; set; }
#else
        public Action<BrokeredMessage,IMessage> PublishMessageFilter { get; set; }
#endif

        protected internal ServiceBusMqMessageProducer(ServiceBusMqMessageFactory parentFactory)
        {
            this.parentFactory = parentFactory;
        }

        public virtual void Dispose()
        {
            StopClients();
        }

        public void StopClients()
        {
            //parentFactory.StopQueues();
        }

        public void Publish<T>(T messageBody)
        {
            // Ensure we're publishing an IMessage
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

        public virtual void Publish(string queueName, IMessage message)
        {
            queueName = queueName.SafeQueueName();
            message.ReplyTo = message.ReplyTo.SafeQueueName();

            var sbClient = parentFactory.GetOrCreateClient(queueName);
            using (JsConfig.With(new Text.Config { IncludeTypeInfo = true }))
            {
                var msgBody = JsonSerializer.SerializeToString(message, typeof(IMessage));
#if NETCORE
                var msg = new Microsoft.Azure.ServiceBus.Message
                {
                    Body = msgBody.ToUtf8Bytes(),
                    MessageId = message.Id.ToString()
                };
                sbClient.SendAsync(ApplyFilter(msg, message)).Wait();
#else
                var msg = new BrokeredMessage(msgBody) { MessageId = message.Id.ToString() };

                sbClient.Send(ApplyFilter(msg, message));
#endif
            }
        }

#if NETCORE
        public Microsoft.Azure.ServiceBus.Message ApplyFilter(Microsoft.Azure.ServiceBus.Message azureMessage, IMessage message)
        {
            PublishMessageFilter?.Invoke(azureMessage, message);
            return azureMessage;
        }
#else
        public BrokeredMessage ApplyFilter(BrokeredMessage azureMessage, IMessage message)
        {
            PublishMessageFilter?.Invoke(azureMessage, message);
            return azureMessage;
        }
#endif

#if NETCORE
        protected MessageReceiver GetOrCreateMessageReceiver(string queueName)
        {
            queueName = queueName.SafeQueueName();

            if (sbReceivers.ContainsKey(queueName))
                return sbReceivers[queueName];

            var messageReceiver = new MessageReceiver(
             parentFactory.address,
             queueName,
             ReceiveMode.ReceiveAndDelete);  //should be ReceiveMode.PeekLock, but it does not delete messages from queue on CompleteAsync()

            sbReceivers.Add(queueName, messageReceiver);
            return messageReceiver;
        }
#endif
    }

}
