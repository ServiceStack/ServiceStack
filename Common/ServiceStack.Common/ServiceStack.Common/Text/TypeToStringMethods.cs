using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using ServiceStack.Common.Extensions;

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
				writeFns[i] = ToStringMethods.GetToStringMethod(propertyInfo.PropertyType);
			}

			return (w, x) => TypeToString(w, x, propertyNames, getterFns, writeFns);
		}

		//public static void TypeToString(TextWriter writer, object value, string[] propertyNames,
		//    Func<object, object>[] getterFns, Action<TextWriter, object>[] toStringFns)
		//{
		//    var sb = new StringBuilder();

		//    var propertyNamesLength = propertyNames.Length;
		//    for (var i = 0; i < propertyNamesLength; i++)
		//    {
		//        var propertyName = propertyNames[i];

		//        var propertyValue = getterFns[i](value);
		//        if (propertyValue == null) continue;

		//        if (sb.Length > 0) sb.Append(StringSerializer.MapItemSeperator);

		//        var propertyValueString = toStringFns[i](propertyValue);

		//        sb.Append(propertyName)
		//            .Append(StringSerializer.MapKeySeperator)
		//            .Append(propertyValueString);
		//    }
		//    sb.Insert(0, StringSerializer.MapStartChar);
		//    sb.Append(TextExtensions.TypeEndChar);

		//    return sb.ToString();
		//}

		public static void TypeToString(TextWriter writer, object value, string[] propertyNames,
			Func<object, object>[] getterFns, Action<TextWriter, object>[] toStringFns)
		{
			writer.Write(StringSerializer.MapStartChar);

			var ranOnce = false;
			var propertyNamesLength = propertyNames.Length;
			for (var i = 0; i < propertyNamesLength; i++)
			{
				var propertyName = propertyNames[i];

				var propertyValue = getterFns[i](value);
				if (propertyValue == null) continue;

				ToStringMethods.WriteMapItemSeperatorIfRanOnce(writer, ref ranOnce);

				writer.Write(propertyName);
				writer.Write(StringSerializer.MapKeySeperator);
				toStringFns[i](writer, propertyValue);
			}

			writer.Write(StringSerializer.MapEndChar);
		}


		public static Func<object, object> GetPropertyValueMethod(Type type, PropertyInfo propertyInfo)
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
	}

}

