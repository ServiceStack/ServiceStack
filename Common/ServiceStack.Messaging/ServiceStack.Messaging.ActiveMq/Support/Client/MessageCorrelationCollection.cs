using System;
using System.Collections.Generic;
using System.Threading;
using ServiceStack.Logging;

namespace ServiceStack.Messaging.ActiveMq.Support.Client
{
    internal class MessageCorrelationCollection
    {
        private const int TIMES_BEFORE_COMPACTING_COLLECTION = 1000;
        private ILog log;
        private readonly object semaphore;
        private readonly Dictionary<string, MessageInfo> messages;
        private readonly Dictionary<string, IReplyAsyncResult> messageAsyncResults;
        private int messagesReceived;

        internal MessageCorrelationCollection()
        {
            semaphore = new object();
            messages = new Dictionary<string, MessageInfo>();
            messageAsyncResults = new Dictionary<string, IReplyAsyncResult>();
            messagesReceived = 0;
            log = LogManager.GetLogger(GetType());
        }

        public Dictionary<string, IReplyAsyncResult> MessageAsyncResults
        {
            get { return messageAsyncResults; }
        }

        public void AddMessage(NMS.IMessage message, TimeSpan messageExpiration)
        {
            lock (semaphore)
            {
                log.DebugFormat("Adding Message to Correlation Collection! To: {0}, CorrelationId: {1}, ReplyTo: {2}",
                    message.NMSDestination, message.NMSCorrelationID, message.NMSReplyTo);
                messages[message.NMSCorrelationID] = new MessageInfo(message, messageExpiration);
                if (messageAsyncResults.ContainsKey(message.NMSCorrelationID))
                {
                    messageAsyncResults[message.NMSCorrelationID].Set();
                }
                if ((++messagesReceived % TIMES_BEFORE_COMPACTING_COLLECTION) == 0)
                {
                    Compact();
                }
            }
        }

        public NMS.IMessage Receive(string correlationId, TimeSpan timeout)
        {
            DateTime start = DateTime.Now;
            while ((DateTime.Now - start) < timeout)
            {
                lock (semaphore)
                {
                    if (messages.ContainsKey(correlationId))
                    {
                        MessageInfo messageInfo = messages[correlationId];
                        messages.Remove(correlationId);
                        messageAsyncResults.Remove(correlationId);
                        return messageInfo.Message;
                    }
                }
                Thread.Sleep(100);
            }
            throw new TimeoutException("No response was received");
        }

        /// <summary>
        /// Incase we need it, to get rid of expired messages.
        /// </summary>
        private void Compact()
        {
            lock (semaphore)
            {
                string[] keys = new string[messages.Count];
                messages.Keys.CopyTo(keys, 0);
                int count = messages.Count;
                for (int i = 0; i < count; i++)
                {
                    string key = keys[i];
                    if (messages[key].IsExpired)
                    {
                        messages.Remove(key);
                        if (messageAsyncResults.ContainsKey(key))
                        {
                            messageAsyncResults.Remove(key);
                        }
                    }
                }
            }
        }
    }
}