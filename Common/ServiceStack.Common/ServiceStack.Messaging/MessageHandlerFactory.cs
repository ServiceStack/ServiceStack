using System;

namespace ServiceStack.Messaging
{
	internal class MessageHandlerFactory<T>
		: IMessageHandlerFactory
	{
		private readonly MessagingServiceBase messageService;
		private readonly Action<IMessage<T>> processMessageFn;
		private readonly Action<Exception> processExceptionFn;

		public MessageHandlerFactory(MessagingServiceBase messageService, Action<IMessage<T>> processMessageFn)
			: this(messageService, processMessageFn, null)
		{
		}

		public MessageHandlerFactory(MessagingServiceBase messageService, Action<IMessage<T>> processMessageFn, Action<Exception> processExceptionEx)
		{
			if (messageService == null)
				throw new ArgumentNullException("messageService");

			if (processMessageFn == null)
				throw new ArgumentNullException("processMessageFn");

			this.messageService = messageService;
			this.processMessageFn = processMessageFn;
			this.processExceptionFn = processExceptionEx;
		}

		public IMessageHandler CreateMessageHandler()
		{
			return new MessageHandler<T>(messageService, processMessageFn, processExceptionFn);
		}
	}
}