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
using System.Text;

namespace ServiceStack.Text.Jsv
{
	public class JsvSerializer<T>
	{
		readonly Dictionary<Type, Func<string, object>> DeserializerCache = 
			new Dictionary<Type, Func<string, object>>();

		public T DeserializeFromString(string value, Type type)
		{
			Func<string, object> parseFn;
			lock (DeserializerCache)
			{
				if (!DeserializerCache.TryGetValue(type, out parseFn))
				{
					var genericType = typeof(T).MakeGenericType(type);
					var mi = genericType.GetMethod(
						"DeserializeFromString", new[] { typeof(string) });
					
					parseFn = (Func<string, object>)Delegate.CreateDelegate(
						typeof(Func<string, object>), mi);

					DeserializerCache.Add(type, parseFn);
				}
			}
			return (T)parseFn(value);
		}

		public T DeserializeFromString(string value)
		{
			if (typeof(T) == typeof(string)) return (T)(object)value;

			return (T)JsvReader<T>.Parse(value);
		}

		public void SerializeToWriter(T value, TextWriter writer)
		{
			JsvWriter<T>.WriteObject(writer, value);
		}

		public string SerializeToString(T value)
		{
			if (value == null) return null;
			if (value is string) return value as string;

			var sb = new StringBuilder(4096);
			using (var writer = new StringWriter(sb))
			{
				JsvWriter<T>.WriteObject(writer, value);
			}
			return sb.ToString();
		}
	}
}