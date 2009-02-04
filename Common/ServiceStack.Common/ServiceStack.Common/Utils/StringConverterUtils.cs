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
		private static readonly ILog log = LogManager.GetLogger(typeof(StringConverterUtils));

		const string PARSE_METHOD = "Parse";
		const char ITEM_SEPERATOR = ',';
		const char KEY_VALUE_SEPERATOR = ':';

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

			public TypeDefinition(Type type)
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

				// Get the static Parse(string) method on the type supplied
				ParseMethod = useType.GetMethod(PARSE_METHOD, BindingFlags.Public | BindingFlags.Static, null,
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
								GenericCollectionArgumentTypes = interfaces[0].GetGenericArguments();
								return;
							}
						}
						throw new NotSupportedException(string.Format("Cannot create type {0} from a string.", useType.Name));
					}
				}
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
				var textValues = text.Split(ITEM_SEPERATOR);
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
				const int KEY_INDEX = 0;
				const int VALUE_INDEX = 1;
				var map = this.TypeConstructor.Invoke(new object[] { });
				var keyTypeConverter = GetTypeDefinition(this.GenericCollectionArgumentTypes[KEY_INDEX]);
				var valueTypeConverter = GetTypeDefinition(this.GenericCollectionArgumentTypes[VALUE_INDEX]);
				foreach (var item in textValue.Split(ITEM_SEPERATOR))
				{
					var keyValuePair = item.Split(KEY_VALUE_SEPERATOR);
					var keyValue = keyTypeConverter.GetValue(keyValuePair[KEY_INDEX]);
					var value = valueTypeConverter.GetValue(keyValuePair[VALUE_INDEX]);
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
					log.WarnFormat("Could not set generic collection '{0}' with values '{1}'\n {2}",
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
			if (type.IsEnum || type == typeof(string))
			{
				return true;
			}

			// Get the static Parse(string) method on the type supplied
			var parseMethodInfo = type.GetMethod(PARSE_METHOD, BindingFlags.Public | BindingFlags.Static, null,
												 new[] { typeof(string) }, null);
			if (parseMethodInfo != null)
			{
				return true;
			}

			var hasStringConstructor = GetTypeStringConstructor(type) != null;

			return !hasStringConstructor;
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

		public static string ToString<T>(T value)
		{
			const string FIELD_SEPERATOR = ",";
			const string KEY_SEPERATOR = ":";
			var ILLEGAL_CHARS = new[] {FIELD_SEPERATOR, KEY_SEPERATOR};

			if (Equals(value, default(T))) return default(T).ToString();

			var type = value.GetType();
			if (type == typeof(string) || type.IsValueType)
			{
				return value.ToString();
			}

			var isCollection = type.IsAssignableFrom(typeof(ICollection)) 
				|| type.FindInterfaces((x,y) => x == typeof(ICollection), null).Length > 0;
			if (isCollection)
			{
				//Get a collection of all the types interfaces, if the interface is generic store the generic definition instead
				var typeInterfaces = type.GetInterfaces().Select(x => x.IsGenericType ? x.GetGenericTypeDefinition() : x)
					.ToDictionary(x => x);

				var isGenericCollection = typeInterfaces.ContainsKey(typeof(ICollection<>));

				if (isGenericCollection)
				{
					var genericArguments = type.FindInterfaces((x, criteria) =>
						x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>), null)
						.SelectMany(x => x.GetGenericArguments()).ToList();

					genericArguments.AddRange(
						type.FindInterfaces((x, criteria) =>
						x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>), null)
						.SelectMany(x => x.GetGenericArguments())
					);
					bool isSupported = genericArguments.All(x => x == typeof(string) || x.IsValueType);
					if (!isSupported)
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
						if (keyValueString.Contains(FIELD_SEPERATOR) || keyValueString.Contains(KEY_SEPERATOR))
						{
							throw new ArgumentException(
								string.Format("collection contains an illegal character: '{0}'", ILLEGAL_CHARS));
						}
						if (sb.Length > 0)
						{
							sb.Append(FIELD_SEPERATOR);
						}
						sb.AppendFormat("{0}{1}{2}", keyString, KEY_SEPERATOR, valueString);
					}
					return sb.ToString();
				}
				else
				{
					var valueCollection = (System.Collections.IEnumerable)value;
					var sb = new StringBuilder();
					foreach (var valueItem in valueCollection)
					{
						var stringValueItem = valueItem.ToString();
						if (stringValueItem.Contains(FIELD_SEPERATOR))
						{
							throw new ArgumentException(
								string.Format("collection contains an illegal character: '{0}'", ILLEGAL_CHARS));
						}
						if (sb.Length > 0)
						{
							sb.Append(FIELD_SEPERATOR);
						}
						sb.Append(stringValueItem);
					}
					return sb.ToString();
				}
			}

			return value.ToString();
		}
	}
}