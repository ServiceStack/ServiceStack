using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ServiceStack.Logging;

namespace ServiceStack.Common.Utils
{
	/// <summary>
	/// Creates an instance of a Type from a string value
	/// </summary>
	public static class StringConverterUtils
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(StringConverterUtils));

		const string ParseMethod = "Parse";
		const string ParseStringArrayMethod = "ParseStringArray";
		const char ItemSeperator = ',';
		const char KeyValueSeperator = ':';

		private class TypeDefinition
		{
			private static Dictionary<Type, TypeDefinition> TypeDefinitionMap { get; set; }

			private Type NullableUnderlyingType { get; set; }
			private Type Type { get; set; }
			private ConstructorInfo TypeConstructor { get; set; }
			private MethodInfo ParseMethod { get; set; }
			private Type[] GenericCollectionArgumentTypes { get; set; }

			static TypeDefinition()
			{
				TypeDefinitionMap = new Dictionary<Type, TypeDefinition>();
			}

			private TypeDefinition(Type type)
			{
				this.Type = type;
				Load();
			}

			public static TypeDefinition GetTypeDefinition(Type type)
			{
				if (!TypeDefinitionMap.ContainsKey(type))
				{
					TypeDefinitionMap[type] = new TypeDefinition(type);
				}
				return TypeDefinitionMap[type];
			}

			/// <summary>
			/// Sets up this instance, binding the artefacts that does the string conversion.
			/// </summary>
			private void Load()
			{
				if (Type.IsEnum || Type == typeof(string))
				{
					return;
				}

				var useType = this.Type;

				this.NullableUnderlyingType = Nullable.GetUnderlyingType(Type);
				if (this.NullableUnderlyingType != null)
				{
					useType = this.NullableUnderlyingType;
					if (useType.IsEnum)
					{
						return;
					}
				}

				if (Type == typeof(string[]))
				{
					ParseMethod = GetType().GetMethod(ParseStringArrayMethod,
						BindingFlags.Public | BindingFlags.Static, null,
						new[] { typeof(string) }, null);

					return;
				}

				// Get the static Parse(string) method on the type supplied
				ParseMethod = useType.GetMethod(StringConverterUtils.ParseMethod, BindingFlags.Public | BindingFlags.Static, null,
											 new[] { typeof(string) }, null);

				if (ParseMethod == null)
				{
					TypeConstructor = GetTypeStringConstructor(useType);
					if (TypeConstructor == null)
					{
						Type[] interfaces = useType.FindInterfaces(
							(t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>), null);

						var isGenericCollection = interfaces.Length > 0;
						if (isGenericCollection)
						{
							TypeConstructor = useType.GetConstructor(Type.EmptyTypes);
							if (TypeConstructor != null)
							{
								var dictionaryDefinition = Type.FindInterfaces((t, critera) =>
									t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>), null).FirstOrDefault();

								var isDictionary = dictionaryDefinition != null;
								if (isDictionary)
								{
									GenericCollectionArgumentTypes = dictionaryDefinition.GetGenericArguments();
									return;
								}

								GenericCollectionArgumentTypes = interfaces[0].GetGenericArguments();
								return;
							}
						}
						throw new NotSupportedException(string.Format("Cannot create type {0} from a string.", useType.Name));
					}
				}
			}

			public static string[] ParseStringArray(string value)
			{
				return string.IsNullOrEmpty(value)
						? new string[0]
						: value.Split(',');
			}

			public object GetValue(string value)
			{
				if (this.Type == typeof(string))
				{
					return value;
				}
				bool isNullValue = string.IsNullOrEmpty(value);
				if (isNullValue && this.Type.IsValueType)
				{
					return Activator.CreateInstance(this.Type);
				}
				if (this.Type.IsEnum)
				{
					return Enum.Parse(Type, value);
				}
				if (this.NullableUnderlyingType != null)
				{
					if (isNullValue)
					{
						return null;
					}
					if (this.NullableUnderlyingType.IsEnum)
					{
						return Enum.Parse(NullableUnderlyingType, value);
					}
				}
				if (this.ParseMethod != null)
				{
					return ParseMethod.Invoke(null, new object[] { value });
				}
				if (this.GenericCollectionArgumentTypes != null)
				{
					var isDictionary = Type.FindInterfaces((t, critera) =>
						t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>), null).Length == 1;

					if (isDictionary)
					{
						return CreateDictionaryFromTextValue(value);
					}
					var isList = this.Type.FindInterfaces((t, critera) =>
						t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>), null).Length == 1;
					
					if (isList)
					{
						return CreateListFromTextValue(value);
					}
				}
				if (this.TypeConstructor != null)
				{
					return this.TypeConstructor.Invoke(new object[] { value });
				}
				throw new NotSupportedException(string.Format("Could not parse text value '{0}' to create type '{1}'", value, this.Type));
			}

			public object CreateListFromTextValue(string text)
			{
				var list = this.TypeConstructor.Invoke(new object[] { });
				if (string.IsNullOrEmpty(text)) return list;
				var textValues = text.Split(ItemSeperator);
				var valueTypeConverter = GetTypeDefinition(this.GenericCollectionArgumentTypes[0]);
				foreach (var textValue in textValues)
				{
					var value = valueTypeConverter.GetValue(textValue);
					SetGenericCollection(list, new[] { value });
				}
				return list;
			}

			public object CreateDictionaryFromTextValue(string textValue)
			{
				const int keyIndex = 0;
				const int valueIndex = 1;
				var map = this.TypeConstructor.Invoke(new object[] { });
				var keyTypeConverter = GetTypeDefinition(this.GenericCollectionArgumentTypes[keyIndex]);
				var valueTypeConverter = GetTypeDefinition(this.GenericCollectionArgumentTypes[valueIndex]);
				foreach (var item in textValue.Split(ItemSeperator))
				{
					var keyValuePair = item.Split(KeyValueSeperator);
					var keyValue = keyTypeConverter.GetValue(keyValuePair[keyIndex]);
					var value = valueTypeConverter.GetValue(keyValuePair[valueIndex]);
					SetGenericCollection(map, new[] { keyValue, value });
				}
				return map;
			}

			private static void SetGenericCollection(object genericObj, object[] genericValues)
			{
				var methodInfo = genericObj.GetType().GetMethod("Add");
				if (methodInfo == null) return;
				try
				{
					methodInfo.Invoke(genericObj, genericValues);
				}
				catch (Exception ex)
				{
					Log.WarnFormat("Could not set generic collection '{0}' with values '{1}'\n {2}",
								   genericObj.GetType().FullName, genericValues, ex.Message);
				}
			}
		}

		/// <summary>
		/// Determines whether the specified type is convertible from string.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>
		/// 	<c>true</c> if the specified type is convertible from string; otherwise, <c>false</c>.
		/// </returns>
		public static bool CanCreateFromString(Type type)
		{
			//True if Enum or string
			if (type.IsEnum || type == typeof(string) || type.UnderlyingSystemType.IsValueType)
			{
				return true;
			}

			// Get the static Parse(string) method on the type supplied
			var parseMethodInfo = type.GetMethod(
				ParseMethod, BindingFlags.Public | BindingFlags.Static, null,
				new[] { typeof(string) }, null);

			if (parseMethodInfo != null)
			{
				return true;
			}

			var hasStringConstructor = GetTypeStringConstructor(type) != null;
			if (hasStringConstructor) return true;

			var isCollection = type.IsAssignableFrom(typeof(ICollection))
				|| type.FindInterfaces((x, y) => x == typeof(ICollection), null).Length > 0;

			if (isCollection)
			{
				var typeInterfaces = type.GetInterfaces().Select(x => x.IsGenericType ? x.GetGenericTypeDefinition() : x)
					.ToDictionary(x => x);

				var isGenericCollection = typeInterfaces.ContainsKey(typeof(ICollection<>));
				if (isGenericCollection)
				{
					return IsGenericCollectionOfValueTypesOrStrings(type);
				}

			}

			return false;
		}

		/// <summary>
		/// Get the type(string) constructor if exists
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		private static ConstructorInfo GetTypeStringConstructor(Type type)
		{
			foreach (var ci in type.GetConstructors())
			{
				var paramInfos = ci.GetParameters();
				bool matchFound = (paramInfos.Length == 1 && paramInfos[0].ParameterType == typeof(string));
				if (matchFound)
				{
					return ci;
				}
			}
			return null;
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

		const string FieldSeperator = ",";
		const string KeySeperator = ":";
		static readonly string[] IllegalChars = new[] { FieldSeperator, KeySeperator };

		public static string ToString<T>(T value)
		{
			if (Equals(value, default(T))) return default(T).ToString();

			var type = value.GetType();
			if (type == typeof(string) || type.IsValueType)
			{
				return value.ToString();
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
					var valueDictionary = (System.Collections.IDictionary)value;
					var sb = new StringBuilder();
					foreach (var key in valueDictionary.Keys)
					{
						var keyString = key.ToString();
						var dictionaryValue = valueDictionary[key];
						var valueString = dictionaryValue != null ? dictionaryValue.ToString() : string.Empty;

						var keyValueString = keyString + valueString;
						if (keyValueString.Contains(FieldSeperator) || keyValueString.Contains(KeySeperator))
						{
							throw new ArgumentException(
								string.Format("collection contains an illegal character: '{0}'", IllegalChars));
						}
						if (sb.Length > 0)
						{
							sb.Append(FieldSeperator);
						}
						sb.AppendFormat("{0}{1}{2}", keyString, KeySeperator, valueString);
					}
					return sb.ToString();
				}
				else
				{
					var valueCollection = (System.Collections.IEnumerable)value;
					return EnumerableToString(valueCollection);
				}
			}

			var isEnumerable = type.IsAssignableFrom(typeof(IEnumerable))
				|| type.FindInterfaces((x, y) => x == typeof(IEnumerable), null).Length > 0;

			if (isEnumerable)
			{
				var valueCollection = (System.Collections.IEnumerable)value;
				return EnumerableToString(valueCollection);
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

		private static string EnumerableToString(IEnumerable valueCollection)
		{
			var sb = new StringBuilder();
			foreach (var valueItem in valueCollection)
			{
				var stringValueItem = valueItem.ToString();
				if (stringValueItem.Contains(FieldSeperator))
				{
					throw new ArgumentException(
						string.Format("collection contains an illegal character: '{0}'", IllegalChars));
				}
				if (sb.Length > 0)
				{
					sb.Append(FieldSeperator);
				}
				sb.Append(stringValueItem);
			}
			return sb.ToString();
		}
	}
}