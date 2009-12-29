using System;
using System.Collections.Generic;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Common.Text
{
	internal class TypeDefinition
	{
		private static Dictionary<Type, TypeDefinition> TypeDefinitionMap { get; set; }

		public bool CanCreateFromString { get { return this.ParseMethod != null; } }
		private Func<object> ParseMethodWhenNull { get; set; }
		private Func<string, object> ParseMethod { get; set; }

		static TypeDefinition()
		{
			TypeDefinitionMap = new Dictionary<Type, TypeDefinition>();
		}

		private static TypeDefinition Create(Type type)
		{
			var typeDef = new TypeDefinition();
			typeDef.Load(type);
			return typeDef;
		}

		public static TypeDefinition GetTypeDefinition(Type type)
		{
			lock (TypeDefinitionMap)
			{
				if (!TypeDefinitionMap.ContainsKey(type))
				{
					TypeDefinitionMap[type] = Create(type);
				}
				return TypeDefinitionMap[type];
			}
		}

		private static Func<object> GetParseMethodWhenNull(Type type, Type nullableType)
		{
			if (type.IsValueType)
			{
				return (() => ParseStringMethods.NullValueType(type));
			}
			if (nullableType != null)
			{
				return (() => ParseStringMethods.NullValueType(nullableType));
			}
			return null;
		}

		/// <summary>
		/// Sets up this instance, binding the artefacts that does the string conversion.
		/// </summary>
		private void Load(Type type)
		{
			var nullableType = Nullable.GetUnderlyingType(type);
			var useType = nullableType ?? type;

			this.ParseMethodWhenNull = GetParseMethodWhenNull(type, nullableType);

			this.ParseMethod = ParseStringMethods.GetParseMethod(useType);
		}

		public object GetValue(string value)
		{
			var isNullValue = string.IsNullOrEmpty(value);
			if (isNullValue && this.ParseMethodWhenNull != null)
			{
				return this.ParseMethodWhenNull();
			}
			if (this.ParseMethod != null)
			{
				return ParseMethod(value);
			}

			throw new NotSupportedException(
				string.Format("Could not parse text value '{0}'", value));
		}

	}
}