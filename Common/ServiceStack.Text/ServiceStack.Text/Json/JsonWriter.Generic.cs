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
using System.IO;
using System.Reflection;
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Json
{
	public static class JsonWriter
	{
		public static readonly JsWriter<JsonTypeSerializer> Instance = new JsWriter<JsonTypeSerializer>();

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
						var genericType = typeof(JsonWriter<>).MakeGenericType(type);
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
	}

	/// <summary>
	/// Implement the serializer using a more static approach
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public static class JsonWriter<T>
	{
		private static readonly Action<TextWriter, object> CacheFn;

		public static Action<TextWriter, object> WriteFn()
		{
			return CacheFn;
		}

		static JsonWriter()
		{
			if (typeof(T) == typeof(object))
			{
				CacheFn = JsonWriter.WriteLateBoundObject;
			}
			else
			{
				CacheFn = JsonWriter.Instance.GetWriteFn<T>();
			}
		}

		public static void WriteObject(TextWriter writer, object value)
		{
			CacheFn(writer, value);
		}
	}

}