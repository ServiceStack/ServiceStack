#if !SL5 
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace ServiceStack.Messaging.Rcon
{
    /// <summary>
    /// Processing client used to interface with ServiceStack and allow a message to be processed.
    /// Not an actual client.
    /// </summary>
    internal class ProcessingClient : IMessageQueueClient, IOneWayClient
    {
        Packet thePacket;
        Socket theClient;
        Server theServer;
        bool givenPacket = false;

        public ProcessingClient(Packet packet, Socket client, Server server)
        {
            thePacket = packet;
            theClient = client;
            theServer = server;
        }

        public void Publish<T>(T messageBody)
        {
            if (typeof(IMessage).IsAssignableFrom(typeof(T)))
                Publish((IMessage)messageBody);
            else
                Publish<T>(new Message<T>(messageBody));
        }

        public void Publish<T>(IMessage<T> message)
        {
            Publish(message.ToInQueueName(), message);
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
            if (requests == null) return;
            foreach (var request in requests)
            {
                SendOneWay(request);
            }
        }

        /// <summary>
        /// Publish the specified message into the durable queue @queueName
        /// </summary>
        public void Publish(string queueName, IMessage message)
        {
            var messageBytes = message.ToBytes();
            theServer.Publish(queueName, messageBytes, theClient, thePacket.Sequence);
        }
        
        /// <summary>
        /// Publish the specified message into the transient queue @queueName
        /// </summary>
        public void Notify(string queueName, IMessage message)
        {
            var messageBytes = message.ToBytes();
            theServer.Notify(queueName, messageBytes, theClient, thePacket.Sequence);
        }

        /// <summary>
        /// Synchronous blocking get.
        /// </summary>
        public IMessage<T> Get<T>(string queueName, TimeSpan? timeOut = null)
        {
            if (givenPacket)
                return null;
            var ret = thePacket.Words[1];
            givenPacket = true;
            return ret.ToMessage<T>();
        }
        
        /// <summary>
        /// Non blocking get message
        /// </summary>
        public IMessage<T> GetAsync<T>(string queueName)
        {
            return Get<T>(queueName, TimeSpan.MinValue);
        }

        public void Ack(IMessage message)
        {
        }

        public void Nak(IMessage message, bool requeue, Exception exception = null)
        {
            var msgEx = exception as MessagingException;
            if (!requeue && msgEx != null && msgEx.ResponseDto != null)
            {
                var msg = MessageFactory.Create(msgEx.ResponseDto);
                Publish(msg.ToDlqQueueName(), msg);
                return;
            }

            var queueName = requeue
                ? message.ToInQueueName()
                : message.ToDlqQueueName();

            Publish(queueName, message);
        }

        public IMessage<T> CreateMessage<T>(object mqResponse)
        {
            return (IMessage<T>)mqResponse;
        }

        public string GetTempQueueName()
        {
            return QueueNames.GetTempQueueName();
        }

        public void Nak(IMessage message)
        {
        }

        public void Dispose()
        {
        }
    }
}
#endif