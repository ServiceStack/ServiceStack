using System;
using System.Threading;
using ServiceStack.Common;

namespace ServiceStack.WebHost.Endpoints
{
	public class AsyncResponseResult : IAsyncResult
	{
		private readonly IAsyncResult _asyncResult;

		public AsyncResponseResult(IAsyncResult asyncResult)
		{
			_asyncResult = asyncResult;
		}

		public bool IsCompleted { get { return _asyncResult.IsCompleted; } }
		public WaitHandle AsyncWaitHandle { get { return _asyncResult.AsyncWaitHandle; } }
		public object AsyncState { get { return _asyncResult.AsyncState; } }
		public bool CompletedSynchronously { get { return _asyncResult.CompletedSynchronously; } }

		public virtual void Process(Action<object> next)
		{
			new Action(() =>
			{
				_asyncResult.AsyncWaitHandle.WaitOne();
				next(_asyncResult);
			}).ExecAsync();
		}
	}
}