using System;

namespace ServiceStack.Messaging
{
    public class MessageHandlerFactory<T>
        : IMessageHandlerFactory
    {
        public const int DefaultRetryCount = 2; //Will be a total of 3 attempts
        private readonly IMessageService messageService;

        public Func<IMessage, IMessage> RequestFilter { get; set; }
        public Func<object, object> ResponseFilter { get; set; }

        private readonly Func<IMessage<T>, object> processMessageFn;
        private readonly Action<IMessage<T>, Exception> processExceptionFn;
        public int RetryCount { get; set; }

        public MessageHandlerFactory(IMessageService messageService, Func<IMessage<T>, object> processMessageFn)
            : this(messageService, processMessageFn, null)
        {
        }

        public MessageHandlerFactory(IMessageService messageService, 
            Func<IMessage<T>, object> processMessageFn,
            Action<IMessage<T>, Exception> processExceptionEx)
        {
            if (messageService == null)
                throw new ArgumentNullException("messageService");

            if (processMessageFn == null)
                throw new ArgumentNullException("processMessageFn");

            this.messageService = messageService;
            this.processMessageFn = processMessageFn;
            this.processExceptionFn = processExceptionEx;
            this.RetryCount = DefaultRetryCount;
        }

        public IMessageHandler CreateMessageHandler()
        {
            if (this.RequestFilter == null && this.ResponseFilter == null)
            {
                return new MessageHandler<T>(messageService, processMessageFn, 
                    processExceptionFn, this.RetryCount);
            }

            return new MessageHandler<T>(messageService, msg => 
                {
                    if (this.RequestFilter != null)
                        msg = (IMessage<T>) this.RequestFilter(msg);

                    var result = this.processMessageFn(msg);

                    if (this.ResponseFilter != null)
                        result = this.ResponseFilter(result);

                    return result;
                },
                processExceptionFn, this.RetryCount);
        }
    }
}