using System;
using System.Collections.Generic;
using System.Net;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
	public static class SupportExtensions
	{
		public static string GetOperationName(this HttpListenerRequest request)
		{
			return request.Url.Segments[request.Url.Segments.Length - 1];
		}
	}
}

namespace ServiceStack.Common.Extensions
{
	public static class SupportExtensions
	{
		public static void ThrowIfNullOrEmpty(this string strValue)
		{
			ThrowIfNullOrEmpty(strValue, null);
		}

		public static void ThrowIfNullOrEmpty(this string strValue, string varName)
		{
			if (string.IsNullOrEmpty(strValue))
				throw new ArgumentNullException(varName ?? "string");
		}

		public static bool AreEqual(this byte[] b1, byte[] b2)
		{
			if (b1 == b2) return true;
			if (b1 == null || b2 == null) return false;
			if (b1.Length != b2.Length) return false;

			for (var i = 0; i < b1.Length; i++)
			{
				if (b1[i] != b2[i]) return false;
			}

			return true;
		}

		public static void ForEach<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, Action<TKey, TValue> onEachFn)
		{
			foreach (var entry in dictionary)
			{
				onEachFn(entry.Key, entry.Value);
			}
		}
	}
}

namespace ServiceStack.Common.Utils
{
	public class ReflectionUtils
	{
		public static object CreateInstance(Type type)
		{
			return Activator.CreateInstance(type);
		}
	}
}