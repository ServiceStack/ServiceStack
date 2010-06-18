//
// http://code.google.com/p/servicestack/wiki/TypeSerializer
// ServiceStack.Text: .NET C# POCO Type Text Serializer.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2010 Liquidbit Ltd.
//
// Licensed under the same terms of ServiceStack: new BSD license.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ServiceStack.Text.Support;

namespace ServiceStack.Text
{
	public static class ReflectionExtensions
	{
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

		public static bool IsInstanceOf(this Type type, Type thisOrBaseType)
		{
			while (type != null)
			{
				if (type == thisOrBaseType)
					return true;

				type = type.BaseType;
			}
			return false;
		}

		public static bool IsGenericType(this Type type)
		{
			while (type != null)
			{
				if (type.IsGenericType)
					return true;

				type = type.BaseType;
			}
			return false;
		}

		public static Type GetGenericType(this Type type)
		{
			while (type != null)
			{
				if (type.IsGenericType)
					return type;

				type = type.BaseType;
			}
			return null;
		}

		public static bool IsOrHasInterfaceOf(this Type type, Type interfaceType)
		{
			var genericType = type.GetGenericType();
			var listInterfaces = type.FindInterfaces((t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == interfaceType, null);
			return listInterfaces.Length > 0 || (genericType != null && genericType.GetGenericTypeDefinition() == interfaceType);
		}

		public static Type GetTypeWithInterfaceOf(this Type type, Type interfaceType)
		{
			if (type == interfaceType) return interfaceType;

			var interfaces = type.FindInterfaces((t, critera) => t == interfaceType, null);

			if (interfaces.Length > 0)
				return interfaces[0];

			return null;
		}

		public static bool IsNumericType(this Type type)
		{
			if (!type.IsValueType) return false;
			return type.IsIntegerType() || type.IsRealNumberType();
		}

		public static bool IsIntegerType(this Type type)
		{
			if (!type.IsValueType) return false;
			var underlyingType = Nullable.GetUnderlyingType(type);
			return underlyingType == typeof (byte)
		       || underlyingType == typeof (sbyte)
		       || underlyingType == typeof (short)
		       || underlyingType == typeof (ushort)
		       || underlyingType == typeof (int)
		       || underlyingType == typeof (uint)
		       || underlyingType == typeof (long)
		       || underlyingType == typeof (ulong);
		}

		public static bool IsRealNumberType(this Type type)
		{
			if (!type.IsValueType) return false;
			var underlyingType = Nullable.GetUnderlyingType(type);
			return underlyingType == typeof(float)
			   || underlyingType == typeof(double)
			   || underlyingType == typeof(decimal);
		}

		public static Type GetTypeWithGenericInterfaceOf(this Type type, Type genericInterfaceType)
		{
			var listInterfaces = type.FindInterfaces(
				(t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == genericInterfaceType, null);

			if (listInterfaces.Length > 0)
				return listInterfaces[0];

			var genericType = type.GetGenericType();
			return genericType.GetGenericTypeDefinition() == genericInterfaceType
					? genericType
					: null;
		}

		public static bool HasAnyTypeDefinitionsOf(this Type genericType, params Type[] theseGenericTypes)
		{
			if (!genericType.IsGenericType) return false;
			var genericTypeDefinition = genericType.GetGenericTypeDefinition();

			foreach (var thisGenericType in theseGenericTypes)
			{
				if (genericTypeDefinition == thisGenericType)
					return true;
			}

			return false;
		}

		public static bool AllHaveInterfacesOfType(
			this Type assignableFromType, params Type[] types)
		{
			foreach (var type in types)
			{
				if (assignableFromType.GetTypeWithInterfaceOf(type) == null) return false;
			}
			return true;
		}

		public static Type[] GetGenericArgumentsIfBothHaveSameGenericDefinitionTypeAndArguments(
			this Type assignableFromType, Type typeA, Type typeB)
		{
			var typeAInterface = typeA.GetTypeWithGenericInterfaceOf(assignableFromType);
			if (typeAInterface == null) return null;

			var typeBInterface = typeB.GetTypeWithGenericInterfaceOf(assignableFromType);
			if (typeBInterface == null) return null;

			var typeAGenericArgs = typeAInterface.GetGenericArguments();
			var typeBGenericArgs = typeBInterface.GetGenericArguments();
			if (typeAGenericArgs.Length != typeBGenericArgs.Length) return null;

			for (var i = 0; i < typeBGenericArgs.Length; i++)
			{
				if (typeAGenericArgs[i] != typeBGenericArgs[i])
				{
					return null;
				}
			}

			return typeAGenericArgs;
		}

		public static TypePair GetGenericArgumentsIfBothHaveConvertibleGenericDefinitionTypeAndArguments(
			this Type assignableFromType, Type typeA, Type typeB)
		{
			var typeAInterface = typeA.GetTypeWithGenericInterfaceOf(assignableFromType);
			if (typeAInterface == null) return null;

			var typeBInterface = typeB.GetTypeWithGenericInterfaceOf(assignableFromType);
			if (typeBInterface == null) return null;

			var typeAGenericArgs = typeAInterface.GetGenericArguments();
			var typeBGenericArgs = typeBInterface.GetGenericArguments();
			if (typeAGenericArgs.Length != typeBGenericArgs.Length) return null;

			for (var i = 0; i < typeBGenericArgs.Length; i++)
			{
				if (!AreAllStringOrValueTypes(typeAGenericArgs[i], typeBGenericArgs[i]))
				{
					return null;
				}
			}

			return new TypePair(typeAGenericArgs, typeBGenericArgs);
		}

		public static bool AreAllStringOrValueTypes(params Type[] types)
		{
			foreach (var type in types)
			{
				if (!(type == typeof(string) || type.IsValueType)) return false;
			}
			return true;
		}

		internal delegate object CtorDelegate();

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
			var dm = new System.Reflection.Emit.DynamicMethod("MyCtor", type, Type.EmptyTypes, typeof(ReflectionExtensions).Module, true);
			var ilgen = dm.GetILGenerator();
			ilgen.Emit(System.Reflection.Emit.OpCodes.Nop);
			ilgen.Emit(System.Reflection.Emit.OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
			ilgen.Emit(System.Reflection.Emit.OpCodes.Ret);

			Func<object> ctorFn = ((CtorDelegate)dm.CreateDelegate(typeof(CtorDelegate))).Invoke;
			return ctorFn;
		}

		public static object CreateInstance(Type type)
		{
			try
			{
				var ctorFn = GetConstructorMethod(type);
				return ctorFn();
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public static PropertyInfo[] GetPublicProperties(this Type type)
		{
			if (type.IsInterface)
			{
				var propertyInfos = new List<PropertyInfo>();

				var considered = new List<Type>();
				var queue = new Queue<Type>();
				considered.Add(type);
				queue.Enqueue(type);
				while (queue.Count > 0)
				{
					var subType = queue.Dequeue();
					foreach (var subInterface in subType.GetInterfaces())
					{
						if (considered.Contains(subInterface)) continue;

						considered.Add(subInterface);
						queue.Enqueue(subInterface);
					}

					var typeProperties = subType.GetProperties(
						BindingFlags.FlattenHierarchy
						| BindingFlags.Public
						| BindingFlags.Instance);

					var newPropertyInfos = typeProperties
						.Where(x => !propertyInfos.Contains(x));

					propertyInfos.InsertRange(0, newPropertyInfos);
				}

				return propertyInfos.ToArray();
			}

			return type.GetProperties(BindingFlags.FlattenHierarchy
				| BindingFlags.Public | BindingFlags.Instance);
		}

		public static PropertyInfo[] GetSerializableProperties(this Type type)
		{
			const string dataContract = "DataContractAttribute";
			const string dataMember = "DataMemberAttribute";

			var publicProperties = GetPublicProperties(type);
			var publicReadableProperties = publicProperties.Where(x => x.GetGetMethod(false) != null);

			//If it is a 'DataContract' only return 'DataMember' properties.
			//checking for "DataContract" using strings to avoid dependency on System.Runtime.Serialization
			var isDataContract = type.GetCustomAttributes(true).Any(x => x.GetType().Name == dataContract);
			if (isDataContract)
			{
				return publicReadableProperties.Where(attr =>
					attr.GetCustomAttributes(false).Any(x => x.GetType().Name == dataMember))
					.ToArray();
			}

			return publicReadableProperties.ToArray();
		}

	}

}