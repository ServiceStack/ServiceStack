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
using System.IO;
using System.Reflection;

namespace ServiceStack.Text.Common
{
	internal static class WriteListsOfElements<TSerializer>
		where TSerializer : ITypeSerializer
	{
		private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

		static readonly Dictionary<Type, WriteObjectDelegate>
			ListCacheFns = new Dictionary<Type, WriteObjectDelegate>();

		public static WriteObjectDelegate GetListWriteFn(Type elementType)
		{
			WriteObjectDelegate writeFn;
			lock (ListCacheFns)
			{
				if (!ListCacheFns.TryGetValue(elementType, out writeFn))
				{
					var genericType = typeof(WriteListsOfElements<,>).MakeGenericType(elementType, typeof(TSerializer));

					var mi = genericType.GetMethod("WriteList", BindingFlags.Static | BindingFlags.Public);

					writeFn = (WriteObjectDelegate)Delegate.CreateDelegate(
															typeof(WriteObjectDelegate), mi);

					ListCacheFns.Add(elementType, writeFn);
				}
			}
			return writeFn;
		}


		static readonly Dictionary<Type, WriteObjectDelegate>
			IListCacheFns = new Dictionary<Type, WriteObjectDelegate>();

		public static WriteObjectDelegate GetIListWriteFn(Type elementType)
		{
			WriteObjectDelegate writeFn;

			lock (IListCacheFns)
			{
				if (!IListCacheFns.TryGetValue(elementType, out writeFn))
				{
					var genericType = typeof(WriteListsOfElements<,>).MakeGenericType(elementType, typeof(TSerializer));

					var mi = genericType.GetMethod("WriteIList", BindingFlags.Static | BindingFlags.Public);

					writeFn = (WriteObjectDelegate)Delegate.CreateDelegate(
															typeof(WriteObjectDelegate), mi);

					IListCacheFns.Add(elementType, writeFn);
				}
			}
			return writeFn;
		}

		static readonly Dictionary<Type, WriteObjectDelegate>
			CacheFns = new Dictionary<Type, WriteObjectDelegate>();

		public static WriteObjectDelegate GetGenericWriteArray(Type elementType)
		{
			WriteObjectDelegate writeFn;

			lock (CacheFns)
			{
				if (!CacheFns.TryGetValue(elementType, out writeFn))
				{
					var genericType = typeof(WriteListsOfElements<,>).MakeGenericType(elementType, typeof(TSerializer));

					var mi = genericType.GetMethod("WriteArray", BindingFlags.Static | BindingFlags.Public);

					writeFn = (WriteObjectDelegate)Delegate.CreateDelegate(
						typeof(WriteObjectDelegate), mi);

					CacheFns.Add(elementType, writeFn);
				}
			}

			return writeFn;
		}

		static readonly Dictionary<Type, WriteObjectDelegate>
			EnumerableCacheFns = new Dictionary<Type, WriteObjectDelegate>();

		public static WriteObjectDelegate GetGenericWriteEnumerable(Type elementType)
		{
			WriteObjectDelegate writeFn;

			lock (EnumerableCacheFns)
			{
				if (!EnumerableCacheFns.TryGetValue(elementType, out writeFn))
				{
					var genericType = typeof(WriteListsOfElements<,>).MakeGenericType(elementType, typeof(TSerializer));

					var mi = genericType.GetMethod("WriteEnumerable", BindingFlags.Static | BindingFlags.Public);

					writeFn = (WriteObjectDelegate)Delegate.CreateDelegate(
															typeof(WriteObjectDelegate), mi);

					EnumerableCacheFns.Add(elementType, writeFn);
				}
			}

			return writeFn;
		}

		static readonly Dictionary<Type, WriteObjectDelegate>
			ListValueTypeCacheFns = new Dictionary<Type, WriteObjectDelegate>();

		public static WriteObjectDelegate GetWriteListValueType(Type elementType)
		{
			WriteObjectDelegate writeFn;

			lock (ListValueTypeCacheFns)
			{
				if (!ListValueTypeCacheFns.TryGetValue(elementType, out writeFn))
				{
					var genericType = typeof(WriteListsOfElements<,>).MakeGenericType(elementType, typeof(TSerializer));

					var mi = genericType.GetMethod("WriteListValueType",
												   BindingFlags.Static | BindingFlags.Public);

					writeFn = (WriteObjectDelegate)Delegate.CreateDelegate(
						typeof(WriteObjectDelegate), mi);

					ListValueTypeCacheFns.Add(elementType, writeFn);
				}
			}

			return writeFn;
		}

		static readonly Dictionary<Type, WriteObjectDelegate>
			IListValueTypeCacheFns = new Dictionary<Type, WriteObjectDelegate>();

		public static WriteObjectDelegate GetWriteIListValueType(Type elementType)
		{
			WriteObjectDelegate writeFn;

			lock (IListValueTypeCacheFns)
			{
				if (!IListValueTypeCacheFns.TryGetValue(elementType, out writeFn))
				{
					var genericType = typeof(WriteListsOfElements<,>).MakeGenericType(elementType, typeof(TSerializer));

					var mi = genericType.GetMethod("WriteIListValueType",
						BindingFlags.Static | BindingFlags.Public);

					writeFn = (WriteObjectDelegate)Delegate.CreateDelegate(
															typeof(WriteObjectDelegate), mi);

					IListValueTypeCacheFns.Add(elementType, writeFn);
				}
			}

			return writeFn;
		}

		public static void WriteIEnumerable(TextWriter writer, object oValueCollection)
		{
			WriteObjectDelegate toStringFn = null;

			writer.Write(JsWriter.ListStartChar);

			var valueCollection = (IEnumerable)oValueCollection;
			var ranOnce = false;
			foreach (var valueItem in valueCollection)
			{
				if (toStringFn == null)
					toStringFn = Serializer.GetWriteFn(valueItem.GetType());

				JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

				toStringFn(writer, valueItem);
			}

			writer.Write(JsWriter.ListEndChar);
		}
	}

	internal static class WriteListsOfElements<T, TSerializer>
		where TSerializer : ITypeSerializer
	{
		private static readonly WriteObjectDelegate ElementWriteFn;

		static WriteListsOfElements()
		{
			ElementWriteFn = JsWriter.GetTypeSerializer<TSerializer>().GetWriteFn<T>();
		}

		public static void WriteList(TextWriter writer, object oList)
		{
			if (oList == null) return;
			WriteGenericIList(writer, (IList<T>)oList);
		}

		public static void WriteGenericList(TextWriter writer, List<T> list)
		{
			writer.Write(JsWriter.ListStartChar);

			var ranOnce = false;
			list.ForEach(x =>
			{
				JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				ElementWriteFn(writer, x);
			});

			writer.Write(JsWriter.ListEndChar);
		}

		public static void WriteListValueType(TextWriter writer, object list)
		{
			WriteGenericListValueType(writer, (List<T>)list);
		}

		public static void WriteGenericListValueType(TextWriter writer, List<T> list)
		{
			writer.Write(JsWriter.ListStartChar);

			var ranOnce = false;
			list.ForEach(x =>
			{
				JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writer.Write(x);
			});

			writer.Write(JsWriter.ListEndChar);
		}

		public static void WriteIList(TextWriter writer, object oList)
		{
			if (oList == null) return;
			WriteGenericIList(writer, (IList<T>)oList);
		}

		public static void WriteGenericIList(TextWriter writer, IList<T> list)
		{
			writer.Write(JsWriter.ListStartChar);

			var ranOnce = false;
			var listLength = list.Count;
			try
			{
				for (var i = 0; i < listLength; i++)
				{
					JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
					ElementWriteFn(writer, list[i]);
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				throw;
			}
			writer.Write(JsWriter.ListEndChar);
		}

		public static void WriteIListValueType(TextWriter writer, object list)
		{
			WriteGenericIListValueType(writer, (IList<T>)list);
		}

		public static void WriteGenericIListValueType(TextWriter writer, IList<T> list)
		{
			writer.Write(JsWriter.ListStartChar);

			var ranOnce = false;
			var listLength = list.Count;
			for (var i = 0; i < listLength; i++)
			{
				JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writer.Write(list[i]);
			}

			writer.Write(JsWriter.ListEndChar);
		}

		public static void WriteArray(TextWriter writer, object oArrayValue)
		{
			if (oArrayValue == null) return;
			WriteGenericArray(writer, (T[])oArrayValue);
		}

		public static void WriteGenericArrayValueType(TextWriter writer, object oArray)
		{
			writer.Write(JsWriter.ListStartChar);

			var array = (T[])oArray;
			var ranOnce = false;
			var arrayLength = array.Length;
			for (var i = 0; i < arrayLength; i++)
			{
				JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writer.Write(array[i]);
			}

			writer.Write(JsWriter.ListEndChar);
		}

		public static void WriteGenericArray(TextWriter writer, T[] array)
		{
			writer.Write(JsWriter.ListStartChar);

			var ranOnce = false;
			var arrayLength = array.Length;
			for (var i = 0; i < arrayLength; i++)
			{
				JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				ElementWriteFn(writer, array[i]);
			}

			writer.Write(JsWriter.ListEndChar);
		}

		public static void WriteEnumerable(TextWriter writer, object oEnumerable)
		{
			if (oEnumerable == null) return;
			WriteGenericEnumerable(writer, (IEnumerable<T>)oEnumerable);
		}

		public static void WriteGenericEnumerable(TextWriter writer, IEnumerable<T> enumerable)
		{
			writer.Write(JsWriter.ListStartChar);

			var ranOnce = false;
			foreach (var value in enumerable)
			{
				JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				ElementWriteFn(writer, value);
			}

			writer.Write(JsWriter.ListEndChar);
		}

		public static void WriteGenericEnumerableValueType(TextWriter writer, IEnumerable<T> enumerable)
		{
			writer.Write(JsWriter.ListStartChar);

			var ranOnce = false;
			foreach (var value in enumerable)
			{
				JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writer.Write(value);
			}

			writer.Write(JsWriter.ListEndChar);
		}
	}

	internal static class WriteLists
	{
		public static void WriteListString(ITypeSerializer serializer, TextWriter writer, object list)
		{
			WriteListString(serializer, writer, (List<string>)list);
		}

		public static void WriteListString(ITypeSerializer serializer, TextWriter writer, List<string> list)
		{
			writer.Write(JsWriter.ListStartChar);

			var ranOnce = false;
			list.ForEach(x =>
			{
				JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				serializer.WriteString(writer, x);
			});

			writer.Write(JsWriter.ListEndChar);
		}

		public static void WriteIListString(ITypeSerializer serializer, TextWriter writer, object list)
		{
			WriteIListString(serializer, writer, (IList<string>)list);
		}

		public static void WriteIListString(ITypeSerializer serializer, TextWriter writer, IList<string> list)
		{
			writer.Write(JsWriter.ListStartChar);

			var ranOnce = false;
			var listLength = list.Count;
			for (var i = 0; i < listLength; i++)
			{
				JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				serializer.WriteString(writer, list[i]);
			}

			writer.Write(JsWriter.ListEndChar);
		}

		public static void WriteBytes(TextWriter writer, object byteValue)
		{
			if (byteValue == null) return;
			writer.Write(Convert.ToBase64String((byte[])byteValue));
		}

		public static void WriteStringArray(ITypeSerializer serializer, TextWriter writer, object oList)
		{
			writer.Write(JsWriter.ListStartChar);

			var list = (string[])oList;
			var ranOnce = false;
			var listLength = list.Length;
			for (var i = 0; i < listLength; i++)
			{
				JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				serializer.WriteString(writer, list[i]);
			}

			writer.Write(JsWriter.ListEndChar);
		}
	}

	internal static class WriteLists<T, TSerializer>
		where TSerializer : ITypeSerializer
	{
		private static readonly WriteObjectDelegate CacheFn;
		private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

		static WriteLists()
		{
			CacheFn = GetWriteFn();
		}

		public static WriteObjectDelegate Write
		{
			get { return CacheFn; }
		}

		public static WriteObjectDelegate GetWriteFn()
		{
			var type = typeof(T);

			var listInterface = type.GetTypeWithGenericTypeDefinitionOf(typeof(IList<>));
			if (listInterface == null)
				throw new ArgumentException(string.Format("Type {0} is not of type IList<>", type.FullName));

			//optimized access for regularly used types
			if (type == typeof(List<string>))
				return (w, x) => WriteLists.WriteListString(Serializer, w, x);
			if (type == typeof(IList<string>))
				return (w, x) => WriteLists.WriteIListString(Serializer, w, x);

			if (type == typeof(List<int>))
				return WriteListsOfElements<int, TSerializer>.WriteListValueType;
			if (type == typeof(IList<int>))
				return WriteListsOfElements<int, TSerializer>.WriteIListValueType;

			if (type == typeof(List<long>))
				return WriteListsOfElements<long, TSerializer>.WriteListValueType;
			if (type == typeof(IList<long>))
				return WriteListsOfElements<long, TSerializer>.WriteIListValueType;


			var elementType = listInterface.GetGenericArguments()[0];

			var isGenericList = typeof(T).IsGenericType
				&& typeof(T).GetGenericTypeDefinition() == typeof(List<>);

			if (elementType.IsValueType
				&& JsWriter.ShouldUseDefaultToStringMethod(elementType))
			{
				if (isGenericList)
					return WriteListsOfElements<TSerializer>.GetWriteListValueType(elementType);

				return WriteListsOfElements<TSerializer>.GetWriteIListValueType(elementType);
			}

			return isGenericList
					? WriteListsOfElements<TSerializer>.GetListWriteFn(elementType)
					: WriteListsOfElements<TSerializer>.GetIListWriteFn(elementType);
		}

	}
}