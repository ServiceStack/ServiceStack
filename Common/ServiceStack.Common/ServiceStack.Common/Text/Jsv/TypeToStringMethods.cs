using System;
using System.IO;
using ServiceStack.Common.Reflection;

namespace ServiceStack.Common.Text.Jsv
{
	public static class TypeToStringMethods<T>
	{
		//public static void ToString(TextWriter writer, object value)
		//{
		//    if (value == null) return;
		//    var writeFn = GetToStringMethod(value.GetType());
		//    writeFn(writer, value);
		//}

		public static Action<TextWriter, object> GetToStringMethod()
		{
			var type = typeof (T);
			if (!type.IsClass) return null;

			var propertyInfos = type.GetProperties();
			if (propertyInfos.Length == 0) return null;

			var propertyInfosLength = propertyInfos.Length;

			var propertyNames = new string[propertyInfos.Length];
			var getterFns = new Func<T, object>[propertyInfosLength];
			var writeFns = new Action<TextWriter, object>[propertyInfosLength];

			for (var i = 0; i < propertyInfosLength; i++)
			{
				var propertyInfo = propertyInfos[i];
				propertyNames[i] = propertyInfo.Name;

				getterFns[i] = propertyInfo.GetValueGetter<T>();

				writeFns[i] = JsvWriter.GetWriteFn(propertyInfo.PropertyType);
			}

			return (w, x) => TypeToString(w, x, propertyNames, getterFns, writeFns);
		}

		public static void TypeToString(TextWriter writer, object value, string[] propertyNames,
			Func<T, object>[] getterFns, Action<TextWriter, object>[] writeFns)
		{
			writer.Write(TypeSerializer.MapStartChar);

			var ranOnce = false;
			var propertyNamesLength = propertyNames.Length;
			for (var i = 0; i < propertyNamesLength; i++)
			{
				var propertyName = propertyNames[i];

				var propertyValue = getterFns[i]((T) value);
				if (propertyValue == null) continue;

				ToStringMethods.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

				writer.Write(propertyName);
				writer.Write(TypeSerializer.MapKeySeperator);
				writeFns[i](writer, propertyValue);
			}

			writer.Write(TypeSerializer.MapEndChar);
		}
	}

}

