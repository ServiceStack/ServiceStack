using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Common.Text
{
	/// <summary>
	/// Creates an instance of a Type from a string value
	/// </summary>
	public static class StringSerializer
	{
		public const char MapStartChar = '{';
		public const char MapKeySeperator = ':';
		public const char MapItemSeperator = ',';
		public const char MapEndChar = '}';
		public const string MapNullValue = "\"\"";

		public const char ListStartChar = '[';
		public const char ListItemSeperator = ',';
		public const char ListEndChar = ']';

		public const char QuoteChar = '"';
		public const string QuoteString = "\"";
		public const string DoubleQuoteString = "\"\"";

		public static readonly char[] CsvChars = new[] { ListItemSeperator, QuoteChar };
		public static readonly char[] EscapeChars = new[] { ListItemSeperator, QuoteChar, MapStartChar, MapEndChar };

		/// <summary>
		/// Determines whether the specified type is convertible from string.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>
		/// 	<c>true</c> if the specified type is convertible from string; otherwise, <c>false</c>.
		/// </returns>
		public static bool CanCreateFromString(Type type)
		{
			var typeDef = TypeDefinition.GetTypeDefinition(type);
			return typeDef.CanCreateFromString;
		}

		/// <summary>
		/// Parses the specified value.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns></returns>
		public static T DeserializeFromString<T>(string value)
		{
			var type = typeof(T);
			return (T)DeserializeFromString(value, type);
		}

		public static T DeserializeFromReader<T>(TextReader reader)
		{
			var type = typeof(T);
			return (T)DeserializeFromString(reader.ReadToEnd(), type);
		}

		/// <summary>
		/// Parses the specified type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="value">The value.</param>
		/// <returns></returns>
		public static object DeserializeFromString(string value, Type type)
		{
			if (type == typeof(string)) return value;
			var typeDefinition = TypeDefinition.GetTypeDefinition(type);
			return typeDefinition.GetValue(value);
		}

		public static object DeserializeFromReader(TextReader reader, Type type)
		{
			return DeserializeFromString(reader.ReadToEnd(), type);
		}

		public static string SerializeToString(object value)
		{
			if (value == null) return null;
			var strValue = value as string;
			if (strValue != null) return strValue;

			var sb = new StringBuilder(4096);
			using (var writer = new StringWriter(sb))
			{
				SerializeToWriter(value, writer);
			}
			return sb.ToString();
		}

		public static void SerializeToWriter(object value, TextWriter writer)
		{
			if (value == null) return;
			var strValue = value as string;
			if (strValue != null)
			{
				writer.Write(strValue);
				return;
			}

			var writeFn = ToStringMethods.GetToStringMethod(value.GetType());
			writeFn(writer, value);
		}

		public static string SerializeToCsv<T>(IEnumerable<T> records)
		{
			var sb = new StringBuilder();
			using (var writer = new StringWriter(sb))
			{
				writer.WriteCsv(records);
				return sb.ToString();
			}
		}
	}

}