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

		/// <summary>
		/// Parses the specified type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="value">The value.</param>
		/// <returns></returns>
		public static object DeserializeFromString(string value, Type type)
		{
			var typeDefinition = TypeDefinition.GetTypeDefinition(type);
			return typeDefinition.GetValue(value);
		}

		public static string SerializeToString(object value)
		{
			if (value == null) return null;
			var toStringMethod = ToStringMethods.GetToStringMethod(value.GetType());
			return toStringMethod(value);
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