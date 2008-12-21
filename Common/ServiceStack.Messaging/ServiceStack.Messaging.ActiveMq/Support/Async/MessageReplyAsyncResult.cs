using System;
using System.Threading;

namespace ServiceStack.Messaging.ActiveMq.Support.Async
{
    internal class MessageReplyAsyncResult : IReplyAsyncResult
    {
        private bool isCompleted;
        private readonly string correlationId;
        private readonly AutoResetEvent waitHandle;

        public MessageReplyAsyncResult(string correlationId)
        {
            isCompleted = false;
            waitHandle = new AutoResetEvent(false);
            this.correlationId = correlationId;
        }

        public bool Set()
        {
            isCompleted = true;
            return waitHandle.Set();
        }

        #region IAsyncResult Members

        public object AsyncState
        {
            get { return correlationId; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get { return waitHandle; }
        }

        public bool CompletedSynchronously
        {
            get { return false; }
        }

        public bool IsCompleted
        {
            get { return isCompleted; }
        }

        #endregion
    }
}
