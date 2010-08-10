//
// http://code.google.com/p/servicestack/wiki/TypeSerializer
// ServiceStack.Text: .NET C# POCO Type Text Serializer.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2010 Liquidbit Ltd.
//
// Licensed under the same terms of ServiceStack: new BSD license.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using ServiceStack.Text.Common;
using ServiceStack.Text.Jsv;

namespace ServiceStack.Text
{
	public static class QueryStringSerializer
	{
		public static readonly JsWriter<JsvTypeSerializer> Instance = new JsWriter<JsvTypeSerializer>();

		private static readonly Dictionary<Type, Action<TextWriter, object>> WriteFnCache =
			new Dictionary<Type, Action<TextWriter, object>>();

		public static Action<TextWriter, object> GetWriteFn(Type type)
		{
			try
			{
				Action<TextWriter, object> writeFn;
				lock (WriteFnCache)
				{
					if (!WriteFnCache.TryGetValue(type, out writeFn))
					{
						var genericType = typeof(QueryStringWriter<>).MakeGenericType(type);
						var mi = genericType.GetMethod("WriteFn",
							BindingFlags.Public | BindingFlags.Static);

						var writeFactoryFn = (Func<Action<TextWriter, object>>)Delegate.CreateDelegate(
							typeof(Func<Action<TextWriter, object>>), mi);

						writeFn = writeFactoryFn();
						WriteFnCache.Add(type, writeFn);
					}
				}
				return writeFn;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				throw;
			}
		}

		public static void WriteLateBoundObject(TextWriter writer, object value)
		{
			if (value == null) return;
			var writeFn = GetWriteFn(value.GetType());
			writeFn(writer, value);
		}

		public static Action<TextWriter, object> GetValueTypeToStringMethod(Type type)
		{
			return Instance.GetValueTypeToStringMethod(type);
		}

		public static string SerializeToString<T>(T value)
		{
			var sb = new StringBuilder(4096);
			using (var writer = new StringWriter(sb, CultureInfo.InvariantCulture))
			{
				GetWriteFn(value.GetType())(writer, value);
			}
			return sb.ToString();
		}
	}

	/// <summary>
	/// Implement the serializer using a more static approach
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public static class QueryStringWriter<T>
	{
		private static readonly Action<TextWriter, object> CacheFn;

		public static Action<TextWriter, object> WriteFn()
		{
			return CacheFn;
		}

		static QueryStringWriter()
		{
			if (typeof(T) == typeof(object))
			{
				CacheFn = QueryStringSerializer.WriteLateBoundObject;
			}
			else
			{
				if (typeof(T).IsClass || typeof(T).IsInterface)
				{
					var canWriteType = WriteType<T, JsvTypeSerializer>.Write;
					if (canWriteType != null)
					{
						CacheFn = WriteType<T, JsvTypeSerializer>.WriteQueryString;
						return;
					}
				}

				CacheFn = QueryStringSerializer.Instance.GetWriteFn<T>();
			}
		}

		public static void WriteObject(TextWriter writer, object value)
		{
			CacheFn(writer, value);
		}
	}
	
}