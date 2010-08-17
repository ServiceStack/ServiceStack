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
using System.Globalization;
using System.IO;
using System.Text;
using ServiceStack.Text.Json;

namespace ServiceStack.Text
{
	/// <summary>
	/// Creates an instance of a Type from a string value
	/// </summary>
	public static class JsonSerializer
	{
		public static T DeserializeFromString<T>(string value)
		{
			if (string.IsNullOrEmpty(value)) return default(T);
			return (T)JsonReader<T>.Parse(value);
		}

		public static T DeserializeFromReader<T>(TextReader reader)
		{
			return DeserializeFromString<T>(reader.ReadToEnd());
		}

		public static object DeserializeFromString(string value, Type type)
		{
			return value == null
					? null
					: JsonReader.GetParseFn(type)(value);
		}

		public static object DeserializeFromReader(TextReader reader, Type type)
		{
			return DeserializeFromString(reader.ReadToEnd(), type);
		}

		public static string SerializeToString<T>(T value)
		{
			if (value == null) return null;
			if (typeof(T) == typeof(string)) return value as string;

			var sb = new StringBuilder(4096);
			using (var writer = new StringWriter(sb, CultureInfo.InvariantCulture))
			{
				JsonWriter<T>.WriteObject(writer, value);
			}
			return sb.ToString();
		}

		public static void SerializeToWriter<T>(T value, TextWriter writer)
		{
			if (value == null) return;
			if (typeof(T) == typeof(string))
			{
				writer.Write(value);
				return;
			}

			JsonWriter<T>.WriteObject(writer, value);
		}

		public static void SerializeToStream<T>(T value, Stream stream)
		{
			using (var writer = new StreamWriter(stream, Encoding.UTF8))
			{
				JsonWriter<T>.WriteObject(writer, value);
			}
		}

		public static T DeserializeFromStream<T>(Stream stream)
		{
			using (var reader = new StreamReader(stream, Encoding.UTF8))
			{
				return DeserializeFromString<T>(reader.ReadToEnd());
			}
		}

	}
}