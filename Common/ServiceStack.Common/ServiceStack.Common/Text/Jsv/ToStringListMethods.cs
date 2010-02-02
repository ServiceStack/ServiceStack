using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Common.Text.Jsv
{
	public static class ToStringListMethodsCache
	{
		static readonly Dictionary<Type, WriteListDelegate> 
			ListCacheFns = new Dictionary<Type, WriteListDelegate>();

		public static Action<TextWriter, object> GetGenericList(Type elementType)
		{
			WriteListDelegate genericDelegate;

			lock (ListCacheFns)
			{
				if (!ListCacheFns.TryGetValue(elementType, out genericDelegate))
				{
					var genericType = typeof(ToStringListMethods<>).MakeGenericType(elementType);
					var mi = genericType.GetMethod("WriteList", BindingFlags.Static | BindingFlags.Public);
					genericDelegate = (WriteListDelegate)Delegate.CreateDelegate(typeof(WriteListDelegate), mi);
					ListCacheFns.Add(elementType, genericDelegate);
				}
			}
			var writeFn = JsvWriter.GetWriteFn(elementType);
			return (w, x) => genericDelegate(w, x, writeFn);
		}

		static readonly Dictionary<Type, WriteListDelegate> 
			IListCacheFns = new Dictionary<Type, WriteListDelegate>();

		public static Action<TextWriter, object> GetGenericIList(Type elementType)
		{
			WriteListDelegate genericDelegate;

			lock (IListCacheFns)
			{
				if (!IListCacheFns.TryGetValue(elementType, out genericDelegate))
				{
					var genericType = typeof(ToStringListMethods<>).MakeGenericType(elementType);
					var mi = genericType.GetMethod("WriteIList", BindingFlags.Static | BindingFlags.Public);
					genericDelegate = (WriteListDelegate)Delegate.CreateDelegate(typeof(WriteListDelegate), mi);
					IListCacheFns.Add(elementType, genericDelegate);
				}
			}
			var writeFn = JsvWriter.GetWriteFn(elementType);
			return (w, x) => genericDelegate(w, x, writeFn);
		}

		static readonly Dictionary<Type, WriteListDelegate> 
			CacheFns = new Dictionary<Type, WriteListDelegate>();

		public static Action<TextWriter, object> GetGenericWriteArray(Type elementType)
		{
			WriteListDelegate genericDelegate;

			lock (CacheFns)
			{
				if (!CacheFns.TryGetValue(elementType, out genericDelegate))
				{
					var genericType = typeof(ToStringListMethods<>).MakeGenericType(elementType);
					var mi = genericType.GetMethod("WriteArray", BindingFlags.Static | BindingFlags.Public);
					genericDelegate = (WriteListDelegate)Delegate.CreateDelegate(typeof(WriteListDelegate), mi);
					CacheFns.Add(elementType, genericDelegate);
				}
			}
			var writeFn = ToStringMethods.GetToStringMethod(elementType);
			return (w, x) => genericDelegate(w, x, writeFn);
		}

		static readonly Dictionary<Type, WriteListDelegate> 
			EnumerableCacheFns = new Dictionary<Type, WriteListDelegate>();

		public static Action<TextWriter, object> GetGenericWriteEnumerable(Type elementType)
		{
			WriteListDelegate genericDelegate;

			lock (EnumerableCacheFns)
			{
				if (!CacheFns.TryGetValue(elementType, out genericDelegate))
				{
					var genericType = typeof(ToStringListMethods<>).MakeGenericType(elementType);
					var mi = genericType.GetMethod("WriteEnumerable", BindingFlags.Static | BindingFlags.Public);
					genericDelegate = (WriteListDelegate)Delegate.CreateDelegate(typeof(WriteListDelegate), mi);
					EnumerableCacheFns.Add(elementType, genericDelegate);
				}
			}
			var writeFn = ToStringMethods.GetToStringMethod(elementType);
			return (w, x) => genericDelegate(w, x, writeFn);
		}

		public static void WriteListString(TextWriter writer, List<string> list)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			list.ForEach(x => {
				ToStringMethods.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writer.Write(x.ToCsvField());
			});

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


		public static void WriteBytes(TextWriter writer, byte[] byteValue)
		{
			if (byteValue == null) return;
			writer.Write(Convert.ToBase64String(byteValue));
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
	}

	public static class ToStringListMethods<T>
	{

		public static void WriteList(TextWriter writer, object oList,
			Action<TextWriter, object> toStringFn)
		{
			if (oList == null) return;
			WriteGenericIList(writer, (IList<T>)oList, toStringFn);
		}

		public static void WriteGenericList(TextWriter writer, List<T> list,
			Action<TextWriter, object> toStringFn)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			list.ForEach(x => {
				ToStringMethods.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				toStringFn(writer, x);
			});

			writer.Write(TypeSerializer.ListEndChar);
		}

		public static void WriteGenericListValueType(TextWriter writer, List<T> list)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			list.ForEach(x => {
				ToStringMethods.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writer.Write(x);
			});

			writer.Write(TypeSerializer.ListEndChar);
		}

		public static void WriteIList(TextWriter writer, object oList,
			Action<TextWriter, object> toStringFn)
		{
			if (oList == null) return;
			WriteGenericIList(writer, (IList<T>)oList, toStringFn);
		}

		public static void WriteGenericIList(TextWriter writer, IList<T> list,
			Action<TextWriter, object> toStringFn)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			var listLength = list.Count;
			for (var i=0; i < listLength; i++)
			{
				ToStringMethods.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				toStringFn(writer, list[i]);
			}

			writer.Write(TypeSerializer.ListEndChar);
		}

		public static void WriteGenericIListValueType(TextWriter writer, IList<T> list)
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

		public static void WriteArray(TextWriter writer, object oArrayValue, Action<TextWriter, object> writeFn)
		{
			if (oArrayValue == null) return;
			WriteGenericArray(writer, (T[])oArrayValue, writeFn);
		}

		public static void WriteGenericArrayValueType(TextWriter writer, T[] array)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			var arrayLength = array.Length;
			for (var i=0; i < arrayLength; i++)
			{
				ToStringMethods.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writer.Write(array[i]);
			}

			writer.Write(TypeSerializer.ListEndChar);
		}

		public static void WriteGenericArray(TextWriter writer, T[] array, Action<TextWriter, object> writeFn)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			var arrayLength = array.Length;
			for (var i=0; i < arrayLength; i++)
			{
				ToStringMethods.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writeFn(writer, array[i]);
			}

			writer.Write(TypeSerializer.ListEndChar);
		}

		public static void WriteEnumerable(TextWriter writer, object oEnumerable, Action<TextWriter, object> writeFn)
		{
			if (oEnumerable == null) return;
			WriteGenericEnumerable(writer, (IEnumerable<T>)oEnumerable, writeFn);
		}

		public static void WriteGenericEnumerable(TextWriter writer, IEnumerable<T> enumerable,
			Action<TextWriter, object> writeFn)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			foreach (var value in enumerable)
			{
				ToStringMethods.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writeFn(writer, value);
			}

			writer.Write(TypeSerializer.ListEndChar);
		}

		public static void WriteGenericEnumerableValueType(TextWriter writer, IEnumerable<T> enumerable)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			foreach (var value in enumerable)
			{
				ToStringMethods.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writer.Write(value);
			}

			writer.Write(TypeSerializer.ListEndChar);
		}

		public static Action<TextWriter, object> GetToStringMethod()
		{
			var type = typeof (T);

			var listInterfaces = type.FindInterfaces(
				(t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>), null);

			if (listInterfaces.Length == 0)
				throw new ArgumentException(string.Format("Type {0} is not of type IList<>", type.FullName));


			//optimized access for regularly used types
			if (type == typeof(List<string>))
				return (w, x) => ToStringListMethodsCache.WriteListString(w, (List<string>) x);
			if (type == typeof(IList<string>))
				return (w, x) => ToStringListMethodsCache.WriteIListString(w, (IList<string>)x);

			if (type == typeof(List<int>))
				return (w, x) => ToStringListMethods<int>.WriteGenericListValueType(w, (List<int>)x);
			if (type == typeof(IList<int>))
				return (w, x) => ToStringListMethods<int>.WriteGenericIListValueType(w, (IList<int>)x);

			if (type == typeof(List<long>))
				return (w, x) => ToStringListMethods<long>.WriteGenericListValueType(w, (List<long>)x);
			if (type == typeof(IList<long>))
				return (w, x) => ToStringListMethods<long>.WriteGenericIListValueType(w, (IList<long>)x);


			var elementType = listInterfaces[0].GetGenericArguments()[0];

			var isGenericList = typeof (T).IsGenericType 
				&& typeof (T).GetGenericTypeDefinition() == typeof (List<>);

			if (elementType.IsValueType
				&& JsvWriter.ShouldUseDefaultToStringMethod(elementType))
			{
				if (isGenericList)
					return (w, x) => WriteGenericListValueType(w, (List<T>) x);
				
				return (w, x) => WriteGenericIListValueType(w, (IList<T>) x);
			}

			return isGenericList 
				? ToStringListMethodsCache.GetGenericList(elementType) 
				: ToStringListMethodsCache.GetGenericIList(elementType);
		}
	}

}