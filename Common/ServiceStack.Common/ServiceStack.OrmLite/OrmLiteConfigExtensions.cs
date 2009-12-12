using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ServiceStack.DataAccess;

namespace ServiceStack.OrmLite
{
	internal static class OrmLiteConfigExtensions
	{
		public const string IdField = "Id";

		private static readonly object ReadLock = new object();

		private static Dictionary<Type, List<FieldDefinition>> TypeFieldDefinitionsMap
			= new Dictionary<Type, List<FieldDefinition>>();

		private static bool IsNullableType(Type theType)
		{
			return (theType.IsGenericType
					&& theType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)));
		}

		/// <summary>
		/// Not using Linq.Where() and manually iterating through objProperties just to avoid dependencies on System.Xml??
		/// </summary>
		/// <param name="objProperties">The obj properties.</param>
		/// <returns></returns>
		internal static bool CheckForIdField(IEnumerable<PropertyInfo> objProperties)
		{
			foreach (var objProperty in objProperties)
			{
				if (objProperty.Name != IdField) continue;
				return true;
			}
			return false;
		}

		internal static void ClearCache()
		{
			TypeFieldDefinitionsMap = new Dictionary<Type, List<FieldDefinition>>();
		}

		internal static List<FieldDefinition> GetFieldDefinitions(this Type type)
		{
			lock (ReadLock)
			{
				List<FieldDefinition> fieldDefinitions;
				if (!TypeFieldDefinitionsMap.TryGetValue(type, out fieldDefinitions))
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

						var uniqueAttrs = propertyInfo.GetCustomAttributes(typeof(IndexAttribute), true);
						var isUnique = uniqueAttrs.Count() > 0 && ((IndexAttribute)uniqueAttrs[0]).Unique;

						var fieldDefinition = new FieldDefinition {
							Name = propertyInfo.Name,
							FieldType = propertyType,
							PropertyInfo = propertyInfo,
							IsNullable = isNullable,
							IsPrimaryKey = isPrimaryKey,
							AutoIncrement = autoIncrement,
							IsUnique = isUnique,
							ConvertValueFn = OrmLiteConfig.DialectProvider.ConvertDbValue,
							QuoteValueFn = OrmLiteConfig.DialectProvider.GetQuotedValue,
                            PropertyInvoker = OrmLiteConfig.PropertyInvoker,
						};

						fieldDefinitions.Add(fieldDefinition);
					}

					TypeFieldDefinitionsMap[type] = fieldDefinitions;
				}

				return fieldDefinitions;
			}
		}

	}
}