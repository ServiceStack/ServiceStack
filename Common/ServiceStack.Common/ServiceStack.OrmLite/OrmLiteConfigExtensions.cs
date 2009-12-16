using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite
{
	internal static class OrmLiteConfigExtensions
	{
		public const string IdField = "Id";

		private static readonly object ReadLock = new object();

		private static Dictionary<Type, List<FieldDefinition>> typeFieldDefinitionsMap
			= new Dictionary<Type, List<FieldDefinition>>();

		private static bool IsNullableType(Type theType)
		{
			return (theType.IsGenericType
					&& theType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)));
		}

		internal static bool CheckForIdField(IEnumerable<PropertyInfo> objProperties)
		{
			// Not using Linq.Where() and manually iterating through objProperties just to avoid dependencies on System.Xml??
			foreach (var objProperty in objProperties)
			{
				if (objProperty.Name != IdField) continue;
				return true;
			}
			return false;
		}

		internal static void ClearCache()
		{
			typeFieldDefinitionsMap = new Dictionary<Type, List<FieldDefinition>>();
		}

		internal static List<FieldDefinition> GetFieldDefinitions(this Type type)
		{
			lock (ReadLock)
			{
				List<FieldDefinition> fieldDefinitions;
				if (!typeFieldDefinitionsMap.TryGetValue(type, out fieldDefinitions))
				{
					fieldDefinitions = new List<FieldDefinition>();

					var objProperties = type.GetProperties(
						BindingFlags.Public | BindingFlags.Instance).ToList();

					var hasIdField = CheckForIdField(objProperties);

					var i = 0;
					foreach (var propertyInfo in objProperties)
					{
						var isFirst = i++ == 0;

						var isPrimaryKey = propertyInfo.Name == IdField
										   || (!hasIdField && isFirst);

						var isNullableType = IsNullableType(propertyInfo.PropertyType);

						var isNullable = !propertyInfo.PropertyType.IsValueType
										 || isNullableType;

						var propertyType = isNullableType
											? Nullable.GetUnderlyingType(propertyInfo.PropertyType)
											: propertyInfo.PropertyType;

						var autoIncrement = isPrimaryKey && propertyType == typeof(int);

						var indexAttr = propertyInfo.GetCustomAttributes(typeof(IndexAttribute), true);
						var isUnique = indexAttr.Length > 0 && ((IndexAttribute)indexAttr[0]).Unique;

						var stringLengthAttrs = propertyInfo.GetCustomAttributes(typeof(StringLengthAttribute), true);
						var fieldLength = stringLengthAttrs.Length > 0 
							? ((StringLengthAttribute) stringLengthAttrs[0]).MaximumLength 
							: (int?) null;

						var defaultValueAttrs = propertyInfo.GetCustomAttributes(typeof(DefaultAttribute), true);
						var defaultValue = defaultValueAttrs.Length > 0
							? ((DefaultAttribute)defaultValueAttrs[0]).DefaultValue
							: null;

						var fieldDefinition = new FieldDefinition {
							Name = propertyInfo.Name,
							FieldType = propertyType,
							PropertyInfo = propertyInfo,
							IsNullable = isNullable,
							IsPrimaryKey = isPrimaryKey,
							AutoIncrement = autoIncrement,
							IsIndexed = indexAttr.Length > 0,
							IsUnique = isUnique,
							FieldLength = fieldLength,
							DefaultValue = defaultValue,
							ConvertValueFn = OrmLiteConfig.DialectProvider.ConvertDbValue,
							QuoteValueFn = OrmLiteConfig.DialectProvider.GetQuotedValue,
                            PropertyInvoker = OrmLiteConfig.PropertyInvoker,
						};

						fieldDefinitions.Add(fieldDefinition);
					}

					typeFieldDefinitionsMap[type] = fieldDefinitions;
				}

				return fieldDefinitions;
			}
		}

	}
}