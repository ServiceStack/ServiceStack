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
	internal static class JsonWriter
	{
		public static readonly JsWriter<JsonTypeSerializer> Instance = new JsWriter<JsonTypeSerializer>();

		private static readonly Dictionary<Type, WriteObjectDelegate> WriteFnCache =
			new Dictionary<Type, WriteObjectDelegate>();

		public static WriteObjectDelegate GetWriteFn(Type type)
		{
			try
			{
				WriteObjectDelegate writeFn;
				lock (WriteFnCache)
				{
					if (!WriteFnCache.TryGetValue(type, out writeFn))
					{
						var genericType = typeof(JsonWriter<>).MakeGenericType(type);
						var mi = genericType.GetMethod("WriteFn",
							BindingFlags.Public | BindingFlags.Static);

						var writeFactoryFn = (Func<WriteObjectDelegate>)Delegate.CreateDelegate(
							typeof(Func<WriteObjectDelegate>), mi);
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

		public static WriteObjectDelegate GetValueTypeToStringMethod(Type type)
		{
			return Instance.GetValueTypeToStringMethod(type);
		}
	}

	/// <summary>
	/// Implement the serializer using a more static approach
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal static class JsonWriter<T>
	{
		private static readonly WriteObjectDelegate CacheFn;

		public static WriteObjectDelegate WriteFn()
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