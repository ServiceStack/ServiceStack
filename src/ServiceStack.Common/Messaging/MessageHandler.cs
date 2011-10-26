using System;
using System.Text;
using ServiceStack.Logging;

namespace ServiceStack.Messaging
{
    /// <summary>
    /// Processes all messages in a Normal and Priority Queue.
    /// Expects to be called in 1 thread. i.e. Non Thread-Safe.
    /// </summary>
    /// <typeparam name="T"></typeparam>
	public class MessageHandler<T>
		: IMessageHandler, IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(MessageHandler<T>));

		public const int DefaultRetryCount = 2; //Will be a total of 3 attempts
		private readonly IMessageService messageService;
		private readonly Func<IMessage<T>, object> processMessageFn;
		private readonly Action<Exception> processExceptionFn;
		private readonly int retryCount;

		public int TotalMessagesProcessed { get; private set; }
		public int TotalMessagesFailed { get; private set; }
		public int TotalRetries { get; private set; }
		public int TotalNormalMessagesReceived { get; private set; }
		public int TotalPriorityMessagesReceived { get; private set; }

		public MessageHandler(IMessageService messageService,
			Func<IMessage<T>, object> processMessageFn)
			: this(messageService, processMessageFn, null, DefaultRetryCount)
		{
		}

		private IMessageQueueClient MqClient { get; set; }
		private Message<T> Message { get; set; }

		public MessageHandler(IMessageService messageService,
			Func<IMessage<T>, object> processMessageFn,
			Action<Exception> processExceptionFn, int retryCount)
		{
			if (messageService == null)
				throw new ArgumentNullException("messageService");

			if (processMessageFn == null)
				throw new ArgumentNullException("processMessageFn");

			this.messageService = messageService;
			this.processMessageFn = processMessageFn;
			this.processExceptionFn = processExceptionFn ?? DefaultExceptionHandler;
			this.retryCount = retryCount;
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
						this.TotalPriorityMessagesReceived++;
						hadReceivedMessages = true;

						var message = messageBytes.ToMessage<T>();
						ProcessMessage(mqClient, message);
					}

					while ((messageBytes = mqClient.GetAsync(QueueNames<T>.In)) != null)
					{
						this.TotalNormalMessagesReceived++;
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

        public IMessageHandlerStats GetStats()
		{
		    return new MessageHandlerStats(typeof(T).Name,
                TotalMessagesProcessed, TotalMessagesFailed, TotalRetries, 
                TotalNormalMessagesReceived, TotalPriorityMessagesReceived);
		}

		private void DefaultExceptionHandler(Exception ex)
		{
			Log.Error("Message exception handler threw an error", ex);

			if (!(ex is UnRetryableMessagingException))
			{
				if (this.Message.RetryAttempts < retryCount)
				{
					this.Message.RetryAttempts++;
					this.TotalRetries++;

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
				TotalMessagesProcessed++;
				mqClient.Notify(QueueNames<T>.Out, this.Message.ToBytes());
			}
			catch (Exception ex)
			{
				try
				{
					TotalMessagesFailed++;
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
			var shouldDispose = messageService as IMessageHandlerDisposer;
			if (shouldDispose != null)
				shouldDispose.DisposeMessageHandler(this);
		}
	}
}