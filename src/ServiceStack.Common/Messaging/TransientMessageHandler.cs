using System;
using ServiceStack.Logging;

namespace ServiceStack.Messaging
{
	internal class TransientMessageHandler<T>
		: IMessageHandler, IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(TransientMessageHandler<T>));

		private readonly TransientMessageServiceBase messageService;
		private readonly Func<IMessage<T>, object> processMessageFn;
		private readonly Action<Exception> processExceptionFn;

		public TransientMessageHandler(TransientMessageServiceBase messageService,
			Func<IMessage<T>, object> processMessageFn)
			: this(messageService, processMessageFn, null)
		{
		}

		private IMessageQueueClient MqClient { get; set; }
		private Message<T> Message { get; set; }

		public TransientMessageHandler(TransientMessageServiceBase messageService,
			Func<IMessage<T>, object> processMessageFn,
			Action<Exception> processExceptionFn)
		{
			if (messageService == null)
				throw new ArgumentNullException("messageService");

			if (processMessageFn == null)
				throw new ArgumentNullException("processMessageFn");

			this.messageService = messageService;
			this.processMessageFn = processMessageFn;
			this.processExceptionFn = processExceptionFn ?? DefaultExceptionHandler;
		}

		public Type MessageType
		{
			get { return typeof(T); }
		}

		public void Process(IMessageQueueClient mqClient)
		{
			try
			{
				bool hadReceivedMessages;
				do
				{
					hadReceivedMessages = false;

					byte[] messageBytes;
					while ((messageBytes = mqClient.GetAsync(QueueNames<T>.Priority)) != null)
					{
						hadReceivedMessages = true;

						var message = messageBytes.ToMessage<T>();
						ProcessMessage(mqClient, message);
					}

					while ((messageBytes = mqClient.GetAsync(QueueNames<T>.In)) != null)
					{
						hadReceivedMessages = true;

						var message = messageBytes.ToMessage<T>();
						ProcessMessage(mqClient, message);
					}

				} while (hadReceivedMessages);

			}
			catch (Exception ex)
			{
				Log.Error("Error serializing message from mq server", ex);
			}
		}

		private void DefaultExceptionHandler(Exception ex)
		{
			Log.Error("Message exception handler threw an error", ex);

			if (!(ex is UnRetryableMessagingException))
			{
				if (this.Message.RetryAttempts < messageService.RetryCount)
				{
					this.Message.RetryAttempts++;

					this.Message.Error = new MessagingException(ex.Message, ex).ToMessageError();
					MqClient.Publish(QueueNames<T>.In, this.Message.ToBytes());
					return;
				}
			}

			MqClient.Publish(QueueNames<T>.Dlq, this.Message.ToBytes());
		}

		public void ProcessMessage(IMessageQueueClient mqClient, Message<T> message)
		{
			this.MqClient = mqClient;
			this.Message = message;

			try
			{
				processMessageFn(message);
				mqClient.Notify(QueueNames<T>.Out, this.Message.ToBytes());
			}
			catch (Exception ex)
			{
				try
				{
					processExceptionFn(ex);
				}
				catch (Exception exHandlerEx)
				{
					Log.Error("Message exception handler threw an error", exHandlerEx);
				}
			}
		}

		public void Dispose()
		{
			messageService.DisposeMessageHandler(this);
		}
	}
}