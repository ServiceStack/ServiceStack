using System;
using System.Collections.Generic;
using System.Threading;
using ServiceStack.Common.Support;
using ServiceStack.Common.Utils;

namespace ServiceStack.Common.Extensions
{
	public static class ActionExecExtensions
	{

		public static List<WaitHandle> ExecAsync(this IEnumerable<Action> actions)
		{
			var waitHandles = new List<WaitHandle>();
			foreach (var action in actions)
			{
				var waitHandle = new AutoResetEvent(false);
				waitHandles.Add(waitHandle);
				var commandExecsHandler = new ActionExecHandler(action, waitHandle);
				ThreadPool.QueueUserWorkItem(x => ((ActionExecHandler)x).Execute(), commandExecsHandler);
			}
			return waitHandles;
		}

		public static void WaitAll(this List<WaitHandle> waitHandles, TimeSpan timeOut)
		{
			WaitAll(waitHandles.ToArray(), timeOut.Milliseconds);
		}

		public static void WaitAll(this List<WaitHandle> waitHandles, int timeOutMs)
		{
			WaitAll(waitHandles.ToArray(), timeOutMs);
		}

		public static void WaitAll(WaitHandle[] waitHandles, int timeOutMs)
		{
			// throws an exception if there are no wait handles
			if (waitHandles != null && waitHandles.Length > 0)
			{
				if (Thread.CurrentThread.ApartmentState == ApartmentState.STA)
				{
					// WaitAll for multiple handles on an STA thread is not supported.
					// CurrentThread is ApartmentState.STA when run under unit tests
					foreach (WaitHandle waitHandle in waitHandles)
					{
						waitHandle.WaitOne(timeOutMs, false);
					}
				}
				else
				{
					if (!WaitHandle.WaitAll(waitHandles, timeOutMs, false))
					{
						throw new TimeoutException();
					}
				}
			}
		}
        
	}

}