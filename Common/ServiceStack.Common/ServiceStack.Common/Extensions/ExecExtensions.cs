using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Logging;

namespace ServiceStack.Common.Extensions
{
	public static class ExecExtensions
	{
		public static void LogError(Type declaringType, string clientMethodName, Exception ex)
		{
			var log = LogManager.GetLogger(declaringType);
			log.Error(string.Format("'{0}' threw an error on {1}: {2}", declaringType.FullName, clientMethodName, ex.Message), ex);
		}

		public static void ExecAll<T>(this IEnumerable<T> instances, Action<T> action)
		{
			foreach (var instance in instances)
			{
				try
				{
					action(instance);
				}
				catch (Exception ex)
				{
					LogError(instance.GetType(), action.GetType().Name, ex);
				}
			}
		}

		public static void ExecAllWithFirstOut<T, TReturn>(this IEnumerable<T> instances, Func<T, TReturn> action, ref TReturn firstResult)
		{
			foreach (var instance in instances)
			{
				try
				{
					var result = action(instance);
					if (!Equals(firstResult, default(TReturn)))
					{
						firstResult = result;
					}
				}
				catch (Exception ex)
				{
					LogError(instance.GetType(), action.GetType().Name, ex);
				}
			}
		}

		public static TReturn ExecReturnFirstWithResult<T, TReturn>(this IEnumerable<T> instances, Func<T, TReturn> action)
		{
			foreach (var instance in instances)
			{
				try
				{
					var result = action(instance);
					if (!Equals(result, default(TReturn)))
					{
						return result;
					}
				}
				catch (Exception ex)
				{
					LogError(instance.GetType(), action.GetType().Name, ex);
				}
			}

			return default(TReturn);
		}

	}
}
