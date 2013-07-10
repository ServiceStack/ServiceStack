using System;
using ServiceStack.Net30.Collections.Concurrent;

namespace ServiceStack.WebHost.Endpoints
{
	/// <summary>
	/// 
	/// </summary>
	public static class AsyncResultFactory
	{
		private static readonly ConcurrentDictionary<Type, Func<object, AsyncResponseResult>> AsyncResults = new ConcurrentDictionary<Type, Func<object, AsyncResponseResult>>();

		public static void RegisterAsyncResult(Type asyncResultType, Func<object, AsyncResponseResult> factory)
		{
			AsyncResults.TryAdd(asyncResultType, factory);
		}

		public static IAsyncResult ProcessAsyncResponse(IAsyncResult asyncResult, Action<object> action)
		{
			Func<object, AsyncResponseResult> factory;
			AsyncResponseResult result = AsyncResults.TryGetValue(asyncResult.GetType(), out factory) ? factory(asyncResult) : new AsyncResponseResult(asyncResult);
			result.Process(action);
			return result;
		}
	}
}