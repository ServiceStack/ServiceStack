using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.Messaging
{
	public abstract class MessagingServiceBase
		: IMessageService
	{
		private bool isRunning;
		
		public int RetryCount { get; protected set; }
		public TimeSpan? RequestTimeOut { get; protected set; }

		public int PoolSize { get; protected set; } //use later

		public abstract IMessageQueueClientFactory MessageFactory { get; }

		protected MessagingServiceBase()
			: this(3, null)
		{
		}

		protected MessagingServiceBase(int retryAttempts, TimeSpan? requestTimeOut)
		{
			this.RetryCount = retryAttempts;
			this.RequestTimeOut = requestTimeOut;
		}

		private readonly Dictionary<Type, IMessageHandlerFactory> handlerMap 
			= new Dictionary<Type, IMessageHandlerFactory>();

		private IMessageHandler[] messageHandlers;

		public void RegisterHandler<T>(Action<IMessage<T>> processMessageFn)
		{
			RegisterHandler(processMessageFn, null);
		}

		public void RegisterHandler<T>(Action<IMessage<T>> processMessageFn, Action<Exception> processExceptionEx)
		{
			if (handlerMap.ContainsKey(typeof(T)))
			{
				throw new ArgumentException("Message handler has already been registered for type: " + typeof(T).Name);
			}

			handlerMap[typeof (T)] = CreateMessageHandlerFactory(processMessageFn, processExceptionEx);
		}

		protected IMessageHandlerFactory CreateMessageHandlerFactory<T>(Action<IMessage<T>> processMessageFn, Action<Exception> processExceptionEx)
		{
			return new MessageHandlerFactory<T>(this, processMessageFn, processExceptionEx);
		}

		public virtual void Start()
		{
			if (isRunning) return;
			
			isRunning = true;

			this.messageHandlers = this.handlerMap.Values.ToList().ConvertAll(
				x => x.CreateMessageHandler()).ToArray();

			using (var mqClient = MessageFactory.CreateMessageQueueClient())
			{
				foreach (var handler in messageHandlers)
				{
					handler.Process(mqClient);
				}
			}

			this.Stop();
		}

		public virtual void Stop()
		{
			isRunning = false;
			messageHandlers = null;
		}

		public virtual void Dispose()
		{
			Stop();
		}

		public virtual void DisposeMessageHandler(IMessageHandler messageHandler)
		{
			lock (messageHandlers)
			{
				if (!isRunning) return;

				var allHandlersAreDisposed = true;
				for (var i = 0; i < messageHandlers.Length; i++)
				{
					if (messageHandlers[i] == messageHandler)
					{
						messageHandlers[i] = null;
					}
					allHandlersAreDisposed = allHandlersAreDisposed 
						&& messageHandlers[i] == null;
				}

				if (allHandlersAreDisposed)
				{
					Stop();
				}
			}
		}
	}
}