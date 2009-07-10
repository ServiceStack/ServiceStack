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

		public static bool WaitAll(this List<WaitHandle> waitHandles, TimeSpan timeOut)
		{
			return WaitAll(waitHandles.ToArray(), timeOut.Milliseconds);
		}

		public static bool WaitAll(this List<WaitHandle> waitHandles, int timeOutMs)
		{
			return WaitAll(waitHandles.ToArray(), timeOutMs);
		}

		public static bool WaitAll(WaitHandle[] waitHandles, int timeOutMs)
		{
			// throws an exception if there are no wait handles
			if (waitHandles == null) throw new ArgumentNullException("waitHandles");
			if (waitHandles.Length == 0) return true;

			if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
			{
				// WaitAll for multiple handles on an STA thread is not supported.
				// CurrentThread is ApartmentState.STA when run under unit tests
				var successfullyComplete = true;
				foreach (var waitHandle in waitHandles)
				{
					successfullyComplete = successfullyComplete 
						&& waitHandle.WaitOne(timeOutMs, false);
				}
				return successfullyComplete;
			}

			return WaitHandle.WaitAll(waitHandles, timeOutMs, false);
		}
        
	}

}