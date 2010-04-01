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

namespace ServiceStack.Text.Jsv
{
	public static class WriteListsOfElements
	{
		static readonly Dictionary<Type, Action<TextWriter, object>> 
			ListCacheFns = new Dictionary<Type, Action<TextWriter, object>>();

		public static Action<TextWriter, object> GetListWriteFn(Type elementType)
		{
			Action<TextWriter, object> writeFn;
			lock (ListCacheFns)
			{
				if (!ListCacheFns.TryGetValue(elementType, out writeFn))
				{
					var genericType = typeof(WriteListsOfElements<>).MakeGenericType(elementType);
					
					var mi = genericType.GetMethod("WriteList", BindingFlags.Static | BindingFlags.Public);
					
					writeFn = (Action<TextWriter, object>)Delegate.CreateDelegate(
						typeof(Action<TextWriter, object>), mi);
					
					ListCacheFns.Add(elementType, writeFn);
				}
			}
			return writeFn;
		}


		static readonly Dictionary<Type, Action<TextWriter, object>> 
			IListCacheFns = new Dictionary<Type, Action<TextWriter, object>>();

		public static Action<TextWriter, object> GetIListWriteFn(Type elementType)
		{
			Action<TextWriter, object> writeFn;

			lock (IListCacheFns)
			{
				if (!IListCacheFns.TryGetValue(elementType, out writeFn))
				{
					var genericType = typeof(WriteListsOfElements<>).MakeGenericType(elementType);
					
					var mi = genericType.GetMethod("WriteIList", BindingFlags.Static | BindingFlags.Public);
					
					writeFn = (Action<TextWriter, object>)Delegate.CreateDelegate(
						typeof(Action<TextWriter, object>), mi);
					
					IListCacheFns.Add(elementType, writeFn);
				}
			}
			return writeFn;
		}

		static readonly Dictionary<Type, Action<TextWriter, object>> 
			CacheFns = new Dictionary<Type, Action<TextWriter, object>>();

		public static Action<TextWriter, object> GetGenericWriteArray(Type elementType)
		{
			Action<TextWriter, object> writeFn;

			lock (CacheFns)
			{
				if (!CacheFns.TryGetValue(elementType, out writeFn))
				{
					var genericType = typeof(WriteListsOfElements<>).MakeGenericType(elementType);
					
					var mi = genericType.GetMethod("WriteArray", BindingFlags.Static | BindingFlags.Public);
					
					writeFn = (Action<TextWriter, object>)Delegate.CreateDelegate(
						typeof(Action<TextWriter, object>), mi);
					
					CacheFns.Add(elementType, writeFn);
				}
			}

			return writeFn;
		}

		static readonly Dictionary<Type, Action<TextWriter, object>> 
			EnumerableCacheFns = new Dictionary<Type, Action<TextWriter, object>>();

		public static Action<TextWriter, object> GetGenericWriteEnumerable(Type elementType)
		{
			Action<TextWriter, object> writeFn;

			lock (EnumerableCacheFns)
			{
				if (!CacheFns.TryGetValue(elementType, out writeFn))
				{
					var genericType = typeof(WriteListsOfElements<>).MakeGenericType(elementType);

					var mi = genericType.GetMethod("WriteEnumerable", BindingFlags.Static | BindingFlags.Public);

					writeFn = (Action<TextWriter, object>)Delegate.CreateDelegate(
						typeof(Action<TextWriter, object>), mi);

					EnumerableCacheFns.Add(elementType, writeFn);
				}
			}

			return writeFn;
		}

		static readonly Dictionary<Type, Action<TextWriter, object>> 
			ListValueTypeCacheFns = new Dictionary<Type, Action<TextWriter, object>>();

		public static Action<TextWriter, object> GetWriteListValueType(Type elementType)
		{
			Action<TextWriter, object> writeFn;

			lock (ListValueTypeCacheFns)
			{
				if (!CacheFns.TryGetValue(elementType, out writeFn))
				{
					var genericType = typeof(WriteListsOfElements<>).MakeGenericType(elementType);

					var mi = genericType.GetMethod("WriteListValueType",
						BindingFlags.Static | BindingFlags.Public);

					writeFn = (Action<TextWriter, object>)Delegate.CreateDelegate(
						typeof(Action<TextWriter, object>), mi);

					ListValueTypeCacheFns.Add(elementType, writeFn);
				}
			}

			return writeFn;
		}

		static readonly Dictionary<Type, Action<TextWriter, object>> 
			IListValueTypeCacheFns = new Dictionary<Type, Action<TextWriter, object>>();

		public static Action<TextWriter, object> GetWriteIListValueType(Type elementType)
		{
			Action<TextWriter, object> writeFn;

			lock (IListValueTypeCacheFns)
			{
				if (!CacheFns.TryGetValue(elementType, out writeFn))
				{
					var genericType = typeof(WriteListsOfElements<>).MakeGenericType(elementType);

					var mi = genericType.GetMethod("WriteIListValueType",
						BindingFlags.Static | BindingFlags.Public);

					writeFn = (Action<TextWriter, object>)Delegate.CreateDelegate(
						typeof(Action<TextWriter, object>), mi);

					IListValueTypeCacheFns.Add(elementType, writeFn);
				}
			}

			return writeFn;
		}
	}

	public static class WriteListsOfElements<T>
	{
		private static readonly Action<TextWriter, object> ElementWriteFn;

		static WriteListsOfElements()
		{
			ElementWriteFn = JsvWriter<T>.WriteFn();
		}

		public static void WriteList(TextWriter writer, object oList)
		{
			if (oList == null) return;
			WriteGenericIList(writer, (IList<T>)oList);
		}

		public static void WriteGenericList(TextWriter writer, List<T> list)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			list.ForEach(x => {
				WriterUtils.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				ElementWriteFn(writer, x);
			});

			writer.Write(TypeSerializer.ListEndChar);
		}

		public static void WriteListValueType(TextWriter writer, object list)
		{
			WriteGenericListValueType(writer, (List<T>) list);
		}

		public static void WriteGenericListValueType(TextWriter writer, List<T> list)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			list.ForEach(x => {
				WriterUtils.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writer.Write(x);
			});

			writer.Write(TypeSerializer.ListEndChar);
		}

		public static void WriteIList(TextWriter writer, object oList)
		{
			if (oList == null) return;
			WriteGenericIList(writer, (IList<T>)oList);
		}

		public static void WriteGenericIList(TextWriter writer, IList<T> list)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			var listLength = list.Count;
			for (var i=0; i < listLength; i++)
			{
				WriterUtils.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				ElementWriteFn(writer, list[i]);
			}

			writer.Write(TypeSerializer.ListEndChar);
		}

		public static void WriteIListValueType(TextWriter writer, object list)
		{
			WriteGenericIListValueType(writer, (IList<T>) list);
		}

		public static void WriteGenericIListValueType(TextWriter writer, IList<T> list)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			var listLength = list.Count;
			for (var i=0; i < listLength; i++)
			{
				WriterUtils.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writer.Write(list[i]);
			}

			writer.Write(TypeSerializer.ListEndChar);
		}

		public static void WriteArray(TextWriter writer, object oArrayValue)
		{
			if (oArrayValue == null) return;
			WriteGenericArray(writer, (T[])oArrayValue);
		}

		public static void WriteGenericArrayValueType(TextWriter writer, object oArray)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var array = (T[])oArray;
			var ranOnce = false;
			var arrayLength = array.Length;
			for (var i=0; i < arrayLength; i++)
			{
				WriterUtils.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writer.Write(array[i]);
			}

			writer.Write(TypeSerializer.ListEndChar);
		}

		public static void WriteGenericArray(TextWriter writer, T[] array)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			var arrayLength = array.Length;
			for (var i=0; i < arrayLength; i++)
			{
				WriterUtils.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				ElementWriteFn(writer, array[i]);
			}

			writer.Write(TypeSerializer.ListEndChar);
		}

		public static void WriteEnumerable(TextWriter writer, object oEnumerable)
		{
			if (oEnumerable == null) return;
			WriteGenericEnumerable(writer, (IEnumerable<T>)oEnumerable);
		}

		public static void WriteGenericEnumerable(TextWriter writer, IEnumerable<T> enumerable)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			foreach (var value in enumerable)
			{
				WriterUtils.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				ElementWriteFn(writer, value);
			}

			writer.Write(TypeSerializer.ListEndChar);
		}

		public static void WriteGenericEnumerableValueType(TextWriter writer, IEnumerable<T> enumerable)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			foreach (var value in enumerable)
			{
				WriterUtils.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writer.Write(value);
			}

			writer.Write(TypeSerializer.ListEndChar);
		}
	}


	public static class WriteLists
	{
		public static void WriteListString(TextWriter writer, object list)
		{
			WriteListString(writer, (List<string>) list);
		}

		public static void WriteListString(TextWriter writer, List<string> list)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			list.ForEach(x => {
				WriterUtils.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writer.Write(x.ToCsvField());
			});

			writer.Write(TypeSerializer.ListEndChar);
		}

		public static void WriteIListString(TextWriter writer, object list)
		{
			WriteIListString(writer, (IList<string>)list);
		}

		public static void WriteIListString(TextWriter writer, IList<string> list)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var ranOnce = false;
			var listLength = list.Count;
			for (var i=0; i < listLength; i++)
			{
				WriterUtils.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writer.Write(list[i].ToCsvField());
			}

			writer.Write(TypeSerializer.ListEndChar);
		}


		public static void WriteBytes(TextWriter writer, object byteValue)
		{
			if (byteValue == null) return;
			writer.Write(Convert.ToBase64String((byte[])byteValue));
		}

		public static void WriteStringArray(TextWriter writer, object oList)
		{
			writer.Write(TypeSerializer.ListStartChar);

			var list = (string[])oList;
			var ranOnce = false;
			var listLength = list.Length;
			for (var i=0; i < listLength; i++)
			{
				WriterUtils.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
				writer.Write(list[i].ToCsvField());
			}

			writer.Write(TypeSerializer.ListEndChar);
		}


		public static void WriteIEnumerable(TextWriter writer, object oValueCollection)
		{
			Action<TextWriter, object> toStringFn = null;

			writer.Write(TypeSerializer.ListStartChar);

			var valueCollection = (IEnumerable)oValueCollection;
			var ranOnce = false;
			foreach (var valueItem in valueCollection)
			{
				if (toStringFn == null)
					toStringFn = JsvWriter.GetWriteFn(valueItem.GetType());

				WriterUtils.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

				toStringFn(writer, valueItem);
			}

			writer.Write(TypeSerializer.ListEndChar);
		}
	}

	public static class WriteLists<T>
	{
		private static readonly Action<TextWriter, object> CacheFn;

		static WriteLists()
		{
			CacheFn = GetWriteFn();
		}

		public static Action<TextWriter, object> Write
		{
			get { return CacheFn; }
		}

		public static Action<TextWriter, object> GetWriteFn()
		{
			var type = typeof(T);

			var listInterfaces = type.FindInterfaces(
				(t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>), null);

			if (listInterfaces.Length == 0)
				throw new ArgumentException(string.Format("Type {0} is not of type IList<>", type.FullName));


			//optimized access for regularly used types
			if (type == typeof(List<string>))
				return WriteLists.WriteListString;
			if (type == typeof(IList<string>))
				return WriteLists.WriteIListString;

			if (type == typeof(List<int>))
				return WriteListsOfElements<int>.WriteListValueType;
			if (type == typeof(IList<int>))
				return WriteListsOfElements<int>.WriteIListValueType;

			if (type == typeof(List<long>))
				return WriteListsOfElements<long>.WriteListValueType;
			if (type == typeof(IList<long>))
				return WriteListsOfElements<long>.WriteIListValueType;


			var elementType = listInterfaces[0].GetGenericArguments()[0];

			var isGenericList = typeof(T).IsGenericType
								&& typeof(T).GetGenericTypeDefinition() == typeof(List<>);

			if (elementType.IsValueType
				&& JsvWriter.ShouldUseDefaultToStringMethod(elementType))
			{
				if (isGenericList)
					return WriteListsOfElements.GetWriteListValueType(elementType);

				return WriteListsOfElements.GetWriteIListValueType(elementType);
			}

			return isGenericList
					? WriteListsOfElements.GetListWriteFn(elementType)
					: WriteListsOfElements.GetIListWriteFn(elementType);
		}

	}
}