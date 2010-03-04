using System;

namespace ServiceStack.Messaging
{
	public interface IMessageService
		: IDisposable
	{
		void RegisterHandler<T>(Action<IMessage<T>> processMessageFn);
		void RegisterHandler<T>(Action<IMessage<T>> processMessageFn, Action<Exception> processExceptionEx);

		void Start();
		void Stop();
	}
}