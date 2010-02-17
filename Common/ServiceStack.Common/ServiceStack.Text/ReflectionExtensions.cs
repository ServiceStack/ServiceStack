using System;
using System.Collections.Generic;

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
			var listInterfaces = type.FindInterfaces(
				(t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == interfaceType, null);

			if (listInterfaces.Length > 0)
				return listInterfaces[0];

			var genericType = type.GetGenericType();			
			return genericType.GetGenericTypeDefinition() == interfaceType
			       	? genericType
			       	: null;
		}

		public static bool HasAnyTypeDefinitionsOf(this Type type, params Type[] genericTypes)
		{
			var thisGenericType = type.GetGenericType();
			if (thisGenericType == null) return false;

			var genericTypeDefinition = thisGenericType.GetGenericTypeDefinition();

			foreach (var genericType in genericTypes)
			{
				if (genericTypeDefinition == genericType)
					return true;
			}

			return false;
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
	}
}