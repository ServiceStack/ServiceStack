#if !SILVERLIGHT 
using System;
using System.Collections.Generic;
using ServiceStack.Messaging;
using System.Net.Sockets;
using System.Text;

namespace ServiceStack.Messaging.Rcon
{
    /// <summary>
    /// Processing client used to interface with ServiceStack and allow a message to be processed.
    /// Not an actual client.
    /// </summary>
    internal class ProcessingClient : IMessageQueueClient
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

        public void Publish(IMessage message)
        {
            var messageBytes = message.ToBytes();
            Publish(new QueueNames(message.Body.GetType()).In, messageBytes);
        }

        public void Publish<T>(IMessage<T> message)
        {
            var messageBytes = message.ToBytes();
            Publish(message.ToInQueueName(), messageBytes);
        }

        /// <summary>
        /// Publish the specified message into the durable queue @queueName
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="messageBytes"></param>
        public void Publish(string queueName, byte[] messageBytes)
        {
            theServer.Publish(queueName, messageBytes, theClient, thePacket.Sequence);
        }
        
        /// <summary>
        /// Publish the specified message into the transient queue @queueName
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="messageBytes"></param>
        public void Notify(string queueName, byte[] messageBytes)
        {
            theServer.Notify(queueName, messageBytes, theClient, thePacket.Sequence);
        }

        /// <summary>
        /// Synchronous blocking get.
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public byte[] Get(string queueName, TimeSpan? timeOut)
        {
            if (givenPacket)
                return null;
            var ret = thePacket.Words[1];
            givenPacket = true;
            return ret;
        }
        
        /// <summary>
        /// Non blocking get message
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public byte[] GetAsync(string queueName)
        {
            return Get(queueName, TimeSpan.MinValue);
        }

        /// <summary>
        /// Blocking wait for notifications on any of the supplied channels
        /// </summary>
        /// <param name="channelNames"></param>
        /// <returns></returns>
        public string WaitForNotifyOnAny(params string[] channelNames)
        {
            return null;
        }

        public void Dispose()
        {
        }
    }
}
#endif