using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Text;

namespace ServiceStack.Common.Utils
{
	/// <summary>
	/// Creates an instance of a Type from a string value
	/// </summary>
	public static class StringConverterUtils
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
		public static T Parse<T>(string value)
		{
			var type = typeof(T);
			return (T)Parse(value, type);
		}

		/// <summary>
		/// Parses the specified type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="value">The value.</param>
		/// <returns></returns>
		public static object Parse(string value, Type type)
		{
			var typeDefinition = TypeDefinition.GetTypeDefinition(type);
			return typeDefinition.GetValue(value);
		}

		//public static string ToString<T>(T value)
		//{
		//    if (Equals(value, default(T))) return default(T).ToString();
		//    var toStringMethod = ToStringMethods.GetToStringMethod(value.GetType());
		//    return toStringMethod(value);
		//}

		public static string ToString(object value)
		{
			if (value == null) return null;
			var toStringMethod = ToStringMethods.GetToStringMethod(value.GetType());
			return toStringMethod(value);
		}

	}
}