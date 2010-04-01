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
using System.IO;
using System.Reflection;
using ServiceStack.Text.Reflection;

namespace ServiceStack.Text.Jsv
{
	public static class WriteType<T>
	{
		private static readonly Action<TextWriter, object> CacheFn;
		private static  TypePropertyWriter[] propertyWriters;

		static WriteType()
		{
			CacheFn = GetWriteFn();
		}

		public static Action<TextWriter, object> Write
		{
			get { return CacheFn; }
		}

		private static Action<TextWriter, object> GetWriteFn()
		{
			var type = typeof (T);
			if (!type.IsClass && !type.IsInterface) return null;

			var propertyInfos = type.GetPublicProperties();
			if (propertyInfos.Length == 0) return null;

			var propertyNamesLength = propertyInfos.Length;

			propertyWriters = new TypePropertyWriter[propertyNamesLength];

			for (var i = 0; i < propertyNamesLength; i++)
			{
				var propertyInfo = propertyInfos[i];

				propertyWriters[i] = new TypePropertyWriter
					(
						propertyInfo.Name,
						propertyInfo.GetValueGetter<T>(),
						JsvWriter.GetWriteFn(propertyInfo.PropertyType),
						i
					);
			}

			return WriteProperties;
		}

		internal class TypePropertyWriter
		{
			private readonly string propertyName;
			private readonly Func<T, object> getterFn;
			private readonly Action<TextWriter, object> writeFn;
			private readonly int index;

			public TypePropertyWriter(string propertyName, 
				Func<T, object> getterFn, Action<TextWriter, object> writeFn, int index)
			{
				this.propertyName = propertyName;
				this.getterFn = getterFn;
				this.writeFn = writeFn;
				this.index = index;
			}

			internal void WriteProperty(TextWriter writer, object value, ref int i)
			{
				var propertyValue = getterFn((T)value);
				if (propertyValue == null) return;

				if (i++ > 0)
					writer.Write(TypeSerializer.ItemSeperator);

				writer.Write(propertyName);
				writer.Write(TypeSerializer.MapKeySeperator);
				writeFn(writer, propertyValue);
			}
		}

		public static void WriteProperties(TextWriter writer, object value)
		{
			writer.Write(TypeSerializer.MapStartChar);
			var i = 0;
			foreach (var propertyWriter in propertyWriters)
			{
				propertyWriter.WriteProperty(writer, value, ref i);
			}
			writer.Write(TypeSerializer.MapEndChar);
		}

		//public static void TypeToString(TextWriter writer, object value)
		//{
		//    writer.Write(TypeSerializer.MapStartChar);

		//    var ranOnce = false;

		//    for (var i = 0; i < propertyNamesLength; i++)
		//    {
		//        var propertyName = propertyNames[i];

		//        var propertyValue = propertyWriters[i]((T) value);
		//        if (propertyValue == null) continue;

		//        WriterUtils.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

		//        writer.Write(propertyName);
		//        writer.Write(TypeSerializer.MapKeySeperator);
		//        writeFns[i](writer, propertyValue);
		//    }

		//    writer.Write(TypeSerializer.MapEndChar);
		//}

	}
}