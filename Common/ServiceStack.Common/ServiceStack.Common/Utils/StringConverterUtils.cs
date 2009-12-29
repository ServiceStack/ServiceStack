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

		public static string ToString<T>(T value)
		{
			if (Equals(value, default(T))) return default(T).ToString();

			var type = value.GetType();
			if (type == typeof(string) || type.IsValueType)
			{
				return ToStringMethods.ToString(value);
			}

			if (type == typeof(byte[]))
			{
				return ToStringMethods.ToString(value as byte[]);
			}

			var isCollection = type.IsAssignableFrom(typeof(ICollection))
				|| type.FindInterfaces((x, y) => x == typeof(ICollection), null).Length > 0;

			if (isCollection)
			{
				//Get a collection of all the types interfaces, if the interface is generic store the generic definition instead
				var typeInterfaces = type.GetInterfaces().Select(x => x.IsGenericType ? x.GetGenericTypeDefinition() : x)
					.ToDictionary(x => x);

				var isGenericCollection = typeInterfaces.ContainsKey(typeof(ICollection<>));
				if (isGenericCollection)
				{
					if (!IsGenericCollectionOfValueTypesOrStrings(type))
					{
						throw new NotSupportedException(
							"Generic collections that contain generic arguments that are not strings or valuetypes are not supported");
					}
				}

				var isDictionary = type.IsAssignableFrom(typeof(IDictionary))
					|| type.FindInterfaces((x, y) => x == typeof(IDictionary), null).Length > 0;

				if (isDictionary)
				{
					return ToStringMethods.ToString((IDictionary)value);
				}
				else
				{
					return ToStringMethods.ToString((IEnumerable)value);
				}
			}

			var isEnumerable = type.IsAssignableFrom(typeof(IEnumerable))
				|| type.FindInterfaces((x, y) => x == typeof(IEnumerable), null).Length > 0;

			if (isEnumerable)
			{
				return ToStringMethods.ToString((IEnumerable)value);
			}

			return value.ToString();
		}

		private static bool IsGenericCollectionOfValueTypesOrStrings(Type type)
		{
			var genericArguments = type.FindInterfaces(
					(x, criteria) => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>)
				, null)
				.SelectMany(x => x.GetGenericArguments()).ToList();

			genericArguments.AddRange(
				type.FindInterfaces((x, criteria) =>
						x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>), null)
					.SelectMany(x => x.GetGenericArguments())
				);

			var isSupported = genericArguments.All(x => x == typeof(string) || x.IsValueType);
			return isSupported;
		}
	}
}