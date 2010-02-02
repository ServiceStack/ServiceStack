using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Common.Text
{

	public static class ToStringListMethods
	{
		public static Action<TextWriter, object> GetToStringMethod<T>()
		{
			var type = typeof(T);

			return GetToStringMethod(type);
		}

		public static Action<TextWriter, object> GetToStringMethod(Type type)
		{
			var listInterfaces = type.FindInterfaces(
				(t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>), null);

			if (listInterfaces.Length == 0)
				throw new ArgumentException(string.Format("Type {0} is not of type IList<>", type.FullName));

			//optimized access for regularly used types
			if (type == typeof(IList<string>))
				return (w, x) => WriteIListString(w, (IList<string>)x);

			if (type == typeof(IList<int>))
				return WriteListValueType<int>;


			var elementType = listInterfaces[0].GetGenericArguments()[0];

			if (elementType.IsValueType)
			{
				return GetValueTypeListToStringMethod(elementType);
			}

			var toStringDelegate = GetGenericListToStringMethod(elementType);
			var writeFn = ToStringMethods.GetToStringMethod(elementType);

			return (w, x) => toStringDelegate(w, x, writeFn);
		}

		public static Action<TextWriter, object> GetValueTypeListToStringMethod(Type elementType)
		{
			var mi = typeof(ToStringListMethods).GetMethod("WriteIList", BindingFlags.Static | BindingFlags.Public);
			WriteDelegate valueTypeWriteDelegate;
			if (!ValueTypeToStringDelegateCache.TryGetValue(elementType, out valueTypeWriteDelegate))
			{
				var genericMi = mi.MakeGenericMethod(new[] { elementType });
				valueTypeWriteDelegate = (WriteDelegate)Delegate.CreateDelegate(typeof(WriteDelegate), genericMi);
				ValueTypeToStringDelegateCache[elementType] = valueTypeWriteDelegate;
			}

			return valueTypeWriteDelegate.Invoke;
		}

		public static WriteListDelegate GetGenericListToStringMethod(Type elementType)
		{
			var mi = typeof(ToStringListMethods).GetMethod("WriteGenericIListObject", BindingFlags.Static | BindingFlags.Public);
			WriteListDelegate @delegate;
			if (!GenericToStringDelegateCache.TryGetValue(elementType, out @delegate))
			{
				var genericMi = mi.MakeGenericMethod(new[] { elementType });
				@delegate = (WriteListDelegate)Delegate.CreateDelegate(typeof(WriteListDelegate), genericMi);
				GenericToStringDelegateCache[elementType] = @delegate;
			}

			return @delegate;
		}

		private static readonly Dictionary<Type, WriteListDelegate> GenericToStringDelegateCache 
			= new Dictionary<Type, WriteListDelegate>();

		private static readonly Dictionary<Type, WriteDelegate> ValueTypeToStringDelegateCache 
			= new Dictionary<Type, WriteDelegate>();


		public static void WriteIEnumerable(TextWriter writer, IEnumerable valueCollection)
		{
			Action<TextWriter, object> toStringFn = null;

			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			foreach (var valueItem in valueCollection)
			{
				if (toStringFn == null)
					toStringFn = ToStringMethods.GetToStringMethod(valueItem.GetType());

				ToStringMethods.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

				toStringFn(writer, valueItem);
			}

			writer.Write(TypeSerializer.ListEndChar);
		}

		public static void WriteIList(TextWriter writer, IList list)
		{
			Action<TextWriter, object> writeFn = null;

			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			var listLength = list.Count;
			for (var i=0; i < listLength; i++)
			{
				var item = list[i];
				if (writeFn == null)
					writeFn = ToStringMethods.GetToStringMethod(item.GetType());

				ToStringMethods.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

				writeFn(writer, item);
			}

			writer.Write(TypeSerializer.ListEndChar);
		}

		public static void WriteStringArray(TextWriter writer, string[] list)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			var listLength = list.Length;
			for (var i=0; i < listLength; i++)
			{
				ToStringMethods.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writer.Write(list[i].ToCsvField());
			}

			writer.Write(TypeSerializer.ListEndChar);
		}

		public static Action<TextWriter, object> GetArrayToStringMethod(Type elementType)
		{
			var mi = typeof(ToStringListMethods).GetMethod("WriteArray",
				BindingFlags.Static | BindingFlags.Public);

			var genericMi = mi.MakeGenericMethod(new[] { elementType });
			var genericDelegate = (WriteListDelegate)Delegate.CreateDelegate(typeof(WriteListDelegate), genericMi);

			var writeFn = ToStringMethods.GetToStringMethod(elementType);
			return (w, x) => genericDelegate(w, x, writeFn);
		}

		public static void WriteArray<T>(TextWriter writer, object oArrayValue, Action<TextWriter, object> writeFn)
		{
			if (oArrayValue == null) return;
			WriteGenericArray(writer, (T[])oArrayValue, writeFn);
		}

		public static void WriteGenericArray<T>(TextWriter writer, T[] list, Action<TextWriter, object> writeFn)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			var listLength = list.Length;
			for (var i=0; i < listLength; i++)
			{
				ToStringMethods.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writeFn(writer, list[i]);
			}

			writer.Write(TypeSerializer.ListEndChar);
		}

		public static void WriteIListString(TextWriter writer, IList<string> list)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			var listLength = list.Count;
			for (var i=0; i < listLength; i++)
			{
				ToStringMethods.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writer.Write(list[i].ToCsvField());
			}

			writer.Write(TypeSerializer.ListEndChar);
		}

		public static void WriteListValueType<T>(TextWriter writer, object oList)
		{
			if (oList == null) return;
			WriteGenericListValueType(writer, (IList<T>)oList);
		}

		public static void WriteGenericListValueType<T>(TextWriter writer, IList<T> list)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			var listLength = list.Count;
			for (var i=0; i < listLength; i++)
			{
				ToStringMethods.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writer.Write(list[i]);
			}

			writer.Write(TypeSerializer.ListEndChar);
		}

		public static void WriteGenericIListObject<T>(TextWriter writer, object oList, Action<TextWriter, object> writeFn)
		{
			if (oList == null) return;
			WriteGenericIList(writer, (IList<T>)oList, writeFn);
		}

		public static void WriteGenericIList<T>(TextWriter writer, IList<T> list, Action<TextWriter, object> writeFn)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			var listLength = list.Count;
			for (var i=0; i < listLength; i++)
			{
				ToStringMethods.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writeFn(writer, list[i]);
			}

			writer.Write(TypeSerializer.ListEndChar);
		}
	}
}