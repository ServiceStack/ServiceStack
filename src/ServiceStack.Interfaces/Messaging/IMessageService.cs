using System;

namespace ServiceStack.Messaging
{
	public interface IMessageService
		: IDisposable
	{
		void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn);
		void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn, Action<Exception> processExceptionEx);

		void Start();
		void Stop();
	}
}