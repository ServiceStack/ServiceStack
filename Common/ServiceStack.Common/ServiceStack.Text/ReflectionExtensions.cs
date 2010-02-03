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
			var ctorFn = GetConstructorMethod(type);
			return ctorFn();
		}
	}
}