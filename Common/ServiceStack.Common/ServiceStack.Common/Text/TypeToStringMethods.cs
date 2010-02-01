using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace ServiceStack.Common.Text
{
	public class TypeToStringMethods
	{
		public static void ToString(TextWriter writer, object value)
		{
			if (value == null) return;
			var writeFn = GetToStringMethod(value.GetType());
			writeFn(writer, value);
		}

		public static Action<TextWriter, object> GetToStringMethod(Type type)
		{
			if (!type.IsClass) return null;

			var propertyInfos = type.GetProperties();
			if (propertyInfos.Length == 0) return null;

			var propertyInfosLength = propertyInfos.Length;
			var propertyNames = new string[propertyInfos.Length];

			var getterFns = new Func<object, object>[propertyInfosLength];
			var writeFns = new Action<TextWriter, object>[propertyInfosLength];

			for (var i = 0; i < propertyInfosLength; i++)
			{
				var propertyInfo = propertyInfos[i];
				propertyNames[i] = propertyInfo.Name;

				getterFns[i] = GetPropertyValueMethod(type, propertyInfo);

				var avoidRecursion = (propertyInfo.PropertyType == type);
				writeFns[i] = avoidRecursion
                    ? (w, x) => GetToStringMethod(type)(w, x)
					: ToStringMethods.GetToStringMethod(propertyInfo.PropertyType);
			}

			return (w, x) => TypeToString(w, x, propertyNames, getterFns, writeFns);
		}

		public static void TypeToString(TextWriter writer, object value, string[] propertyNames,
			Func<object, object>[] getterFns, Action<TextWriter, object>[] writeFns)
		{
			writer.Write(TypeSerializer.MapStartChar);

			var ranOnce = false;
			var propertyNamesLength = propertyNames.Length;
			for (var i = 0; i < propertyNamesLength; i++)
			{
				var propertyName = propertyNames[i];

				var propertyValue = getterFns[i](value);
				if (propertyValue == null) continue;

				ToStringMethods.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

				writer.Write(propertyName);
				writer.Write(TypeSerializer.MapKeySeperator);
				writeFns[i](writer, propertyValue);
			}

			writer.Write(TypeSerializer.MapEndChar);
		}

#if !STATIC_ONLY
		public static Func<object, object> GetPropertyValueMethod(
			Type type, PropertyInfo propertyInfo)
		{
			var getMethodInfo = propertyInfo.GetGetMethod();
			var oInstanceParam = Expression.Parameter(typeof(object), "oInstanceParam");
			var instanceParam = Expression.Convert(oInstanceParam, type);

			var exprCallPropertyGetFn = Expression.Call(instanceParam, getMethodInfo);
			var oExprCallPropertyGetFn = Expression.Convert(exprCallPropertyGetFn, typeof(object));

			var propertyGetFn = Expression.Lambda<Func<object, object>>
			(
				oExprCallPropertyGetFn,
				oInstanceParam
			).Compile();

			return propertyGetFn;
		}
#else
		public static Func<object, object> GetPropertyValueMethod(
			Type type, PropertyInfo propertyInfo)
		{
			var mi = typeof(TypeToStringMethods).GetMethod("CreateFunc");

			var genericMi = mi.MakeGenericMethod(type, propertyInfo.PropertyType);
			var del = genericMi.Invoke(null, new[] { propertyInfo.GetGetMethod() });

			return (Func<object, object>)del;
		}

		public static Func<object, object> CreateFunc<T1, T2>(MethodInfo mi)
		{
			if (mi == null) return null;
			var del = (Func<T1, T2>)Delegate.CreateDelegate(typeof(Func<T1, T2>), mi);
			return x => del((T1)x);
		}
#endif

	}

}

