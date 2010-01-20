using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ServiceStack.Common.Support;
using ServiceStack.Logging;

namespace ServiceStack.Common.Utils
{
	public class ReflectionUtils
	{
		public static readonly ILog Log = LogManager.GetLogger(typeof(ReflectionUtils));

		/// <summary>
		/// Populate an object with Example data.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static object PopulateObject(object obj)
		{
			if (obj == null) return null;

			var members = obj.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
			foreach (var info in members)
			{
				var fieldInfo = info as FieldInfo;
				var propertyInfo = info as PropertyInfo;
				if (fieldInfo != null || propertyInfo != null)
				{
					var memberType = fieldInfo != null ? fieldInfo.FieldType : propertyInfo.PropertyType;
					var value = CreateDefaultValue(memberType);
					SetValue(fieldInfo, propertyInfo, obj, value);
				}
			}
			return obj;
		}

		private static readonly Dictionary<Type, object> DefaultValueTypes 
			= new Dictionary<Type, object>();

		public static object GetDefaultValue(Type type)
		{
			if (!type.IsValueType) return null;

			object defaultValue;
			lock (DefaultValueTypes)
			{
				if (!DefaultValueTypes.TryGetValue(type, out defaultValue))
				{
					defaultValue = Activator.CreateInstance(type);
					DefaultValueTypes[type] = defaultValue;
				}
			} 

			return defaultValue;
		}

		private static readonly Dictionary<string, AssignmentDefinition> AssignmentDefinitionCache 
			= new Dictionary<string, AssignmentDefinition>();

		public static AssignmentDefinition GetAssignmentDefinition(Type toType, Type fromType)
		{
			var cacheKey = toType.FullName + "<" + fromType.FullName;

			lock (AssignmentDefinitionCache)
			{
				AssignmentDefinition definition;
				if (AssignmentDefinitionCache.TryGetValue(cacheKey, out definition))
				{
					return definition;
				}

				definition = new AssignmentDefinition {
					ToType = toType,
					FromType = fromType,
				};

				var members = fromType.GetMembers(BindingFlags.Public | BindingFlags.Instance);
				foreach (var info in members)
				{
					var fromPropertyInfo = info as PropertyInfo;
					if (fromPropertyInfo != null)
					{
						var toPropertyInfo = GetPropertyInfo(toType, fromPropertyInfo.Name);
						if (toPropertyInfo == null) continue;

						if (!fromPropertyInfo.CanRead) continue;
						if (!toPropertyInfo.CanWrite) continue;

						definition.AddMatch(fromPropertyInfo, toPropertyInfo);
					}

					var fromFieldInfo = info as FieldInfo;
					if (fromFieldInfo != null)
					{
						var toFieldInfo = GetFieldInfo(toType, fromFieldInfo.Name);
						if (toFieldInfo == null) continue;

						definition.AddMatch(fromFieldInfo, toFieldInfo);
					}
				}

				AssignmentDefinitionCache[cacheKey] = definition;

				return definition;
			}
		}

		public static To PopulateObject<To, From>(To to, From from)
		{
			if (Equals(to, default(To)) || Equals(from, default(From))) return default(To);

			var assignmentDefinition = GetAssignmentDefinition(to.GetType(), from.GetType());

			assignmentDefinition.Populate(to, from);

			return to;
		}

		public static To PopulateWithNonDefaultValues<To, From>(To to, From from)
		{
			if (Equals(to, default(To)) || Equals(from, default(From))) return default(To);

			var assignmentDefinition = GetAssignmentDefinition(to.GetType(), from.GetType());

			assignmentDefinition.PopulateWithNonDefaultValues(to, from);

			return to;
		}

		public static To PopulateFromPropertiesWithAttribute<To, From>(To to, From from, 
			Type attributeType)
		{
			if (Equals(to, default(To)) || Equals(from, default(From))) return default(To);

			var assignmentDefinition = GetAssignmentDefinition(to.GetType(), from.GetType());

			assignmentDefinition.PopulateFromPropertiesWithAttribute(to, from, attributeType);

			return to;
		}

		/// <summary>
		/// Populate an instance of the type with Example data.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static object PopulateType(Type type)
		{
			var obj = Activator.CreateInstance(type);
			return PopulateObject(obj);
		}

		public static void SetProperty(object obj, PropertyInfo propertyInfo, object value)
		{
			if (!propertyInfo.CanWrite)
			{
				Log.WarnFormat("Attempted to set read only property '{0}'", propertyInfo.Name);
				return;
			}
			var propertySetMetodInfo = propertyInfo.GetSetMethod();
			if (propertySetMetodInfo != null)
			{
				propertySetMetodInfo.Invoke(obj, new[] { value });
			}
		}

		public static void SetValue(FieldInfo fieldInfo, PropertyInfo propertyInfo, object obj, object value)
		{
			try
			{
				if (IsUnsettableValue(fieldInfo, propertyInfo)) return;
				if (fieldInfo != null && !fieldInfo.IsLiteral)
				{
					fieldInfo.SetValue(obj, value);
				}
				else
				{
					SetProperty(obj, propertyInfo, value);
				}
				PopulateObject(value);
			}
			catch (Exception ex)
			{
				var name = (fieldInfo != null) ? fieldInfo.Name : propertyInfo.Name;
				Log.DebugFormat("Could not set member: {0}. Error: {1}", name, ex.Message);
			}
		}

		public static bool IsUnsettableValue(FieldInfo fieldInfo, PropertyInfo propertyInfo)
		{
			if (propertyInfo != null && propertyInfo.ReflectedType != null)
			{
				// Properties on non-user defined classes should not be set
				// Currently we define those properties as properties declared on
				// types defined in mscorlib

				if (propertyInfo.DeclaringType.Assembly == typeof(object).Assembly)
				{
					return true;
				}
			}

			return false;
		}

		public static object[] CreateDefaultValues(IEnumerable<Type> types)
		{
			var values = new List<object>();
			foreach (var type in types)
			{
				values.Add(CreateDefaultValue(type));
			}
			return values.ToArray();
		}

		public static object CreateDefaultValue(Type type)
		{
			if (type == typeof(string))
			{
				return type.Name;
			}

			if (type.IsValueType)
			{
				return Activator.CreateInstance(type);
			}

			if (type.IsArray)
			{
				return PopulateArray(type);
			}

			var constructorInfo = type.GetConstructor(Type.EmptyTypes);
			var hasEmptyConstructor = constructorInfo != null;

			if (hasEmptyConstructor)
			{
				var value = constructorInfo.Invoke(new object[0]);

				Type[] interfaces = type.FindInterfaces((t, critera) =>
					t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>)
					, null);

				bool isGenericCollection = interfaces.Length > 0;

				if (isGenericCollection)
				{
					SetGenericCollection(interfaces[0], type, value);
				}

				return value;
			}
			return null;
		}

		public static void SetGenericCollection(Type realisedListType, Type type, object genericObj)
		{
			var args = realisedListType.GetGenericArguments();

			if (args.Length != 1)
			{
				Log.ErrorFormat("Found a generic list that does not take one generic argument: {0}", realisedListType);

				return;
			}

			var methodInfo = type.GetMethod("Add");

			if (methodInfo != null)
			{
				var argValues = CreateDefaultValues(args);

				methodInfo.Invoke(genericObj, argValues);

				Log.DebugFormat("Added value '{0}' to type '{1}", argValues, genericObj.GetType());
			}
		}

		public static Array PopulateArray(Type type)
		{
			var objArray = Array.CreateInstance(type, 1);
			var elementType = objArray.GetType().GetElementType();
			var objElementType = PopulateType(elementType);
			objArray.SetValue(objElementType, 0);
			PopulateObject(objElementType);
			return objArray;
		}

		//TODO: replace with InAssignableFrom
		public static bool CanCast(Type toType, Type fromType)
		{
			if (toType.IsInterface)
			{
				var interfaceList = fromType.GetInterfaces().ToList();
				if (interfaceList.Contains(toType)) return true;
			}
			else
			{
				Type baseType = fromType;
				bool areSameTypes;
				do
				{
					areSameTypes = baseType == toType;
				}
				while (!areSameTypes && (baseType = fromType.BaseType) != null);

				if (areSameTypes) return true;
			}

			return false;
		}

		public static MemberInfo GetMemberInfo(Type fromType, string memberName)
		{
			var baseType = fromType;
			do
			{
				var members = baseType.GetMembers(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				foreach (var memberInfo in members)
				{
					if (memberInfo.Name == memberName) return memberInfo;
				}
			}
			while ((baseType = baseType.BaseType) != null);
			return null;
		}

		public static FieldInfo GetFieldInfo(Type fromType, string fieldName)
		{
			var baseType = fromType;
			do
			{
				var fieldInfos = baseType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				foreach (var fieldInfo in fieldInfos)
				{
					if (fieldInfo.Name == fieldName) return fieldInfo;
				}
			}
			while ((baseType = baseType.BaseType) != null);
			return null;
		}

		public static PropertyInfo GetPropertyInfo(Type fromType, string propertyName)
		{
			var baseType = fromType;
			do
			{
				var propertyInfos = baseType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				foreach (var propertyInfo in propertyInfos)
				{
					if (propertyInfo.Name == propertyName) return propertyInfo;
				}
			}
			while ((baseType = baseType.BaseType) != null);
			return null;
		}

		public static IEnumerable<KeyValuePair<PropertyInfo, T>> GetPropertyAttributes<T>(Type fromType) where T : Attribute
		{
			var attributeType = typeof(T);
			var baseType = fromType;
			do
			{
				var propertyInfos = baseType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				foreach (var propertyInfo in propertyInfos)
				{
					var attributes = propertyInfo.GetCustomAttributes(attributeType, true);
					foreach (T attribute in attributes)
					{
						yield return new KeyValuePair<PropertyInfo, T>(propertyInfo, attribute);
					}
				}
			}
			while ((baseType = baseType.BaseType) != null);
		}

		public delegate object CtorDelegate();

		static readonly Dictionary<Type, Func<object>> ConstructorMethods = new Dictionary<Type, Func<object>>();
		public static Func<object> GetConstructorMethod(Type type)
		{
			lock (ConstructorMethods)
			{
				Func<object> ctorFn;
				if (!ConstructorMethods.TryGetValue(type, out ctorFn))
				{
					ctorFn = GetConstructorMethodToCache(type);
					ConstructorMethods[type] = ctorFn;
				}
				return ctorFn;
			}
		}

		public static Func<object> GetConstructorMethodToCache(Type type)
		{
			var dm = new DynamicMethod("MyCtor", type, Type.EmptyTypes, typeof(ReflectionUtils).Module, true);
			var ilgen = dm.GetILGenerator();
			ilgen.Emit(OpCodes.Nop);
			ilgen.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
			ilgen.Emit(OpCodes.Ret);

			Func<object> ctorFn = ((CtorDelegate) dm.CreateDelegate(typeof (CtorDelegate))).Invoke;
			return ctorFn;
		}

		public static object CreateInstance(Type type)
		{
			var ctorFn = GetConstructorMethod(type);
			return ctorFn();
		}
	}
}