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
			if (!typeof(T).IsClass) return null;

			var typePropertyAccessors = TypeSerializerPropertyAccessor<T>.AllPropertyAccessors;
			if (typePropertyAccessors.Length == 0) return null;

			var propertyInfosLength = typePropertyAccessors.Length;
			var propertyNames = new string[typePropertyAccessors.Length];

			var getterFns = new Func<T, object>[propertyInfosLength];
			var writeFns = new Action<TextWriter, object>[propertyInfosLength];

			for (var i = 0; i < propertyInfosLength; i++)
			{
				var propertyAccessor = typePropertyAccessors[i];
				propertyNames[i] = propertyAccessor.Name;

				getterFns[i] = propertyAccessor.GetPropertyFn();

				writeFns[i] = JsvWriter.GetWriteFn(propertyAccessor.PropertyType);
			}

			return (w, x) => TypeToString(w, (T) x, propertyNames, getterFns, writeFns);
		}

		public static void TypeToString(TextWriter writer, T value, string[] propertyNames,
			Func<T, object>[] getterFns, Action<TextWriter, object>[] writeFns)
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

	}

}

