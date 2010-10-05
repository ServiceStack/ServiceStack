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
	internal delegate void WriteMapDelegate(
		TextWriter writer,
		object oMap,
		WriteObjectDelegate writeKeyFn,
		WriteObjectDelegate writeValueFn);

	internal static class WriteDictionary<TSerializer>
		where TSerializer : ITypeSerializer
	{
		private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

		internal class MapKey
		{
			internal Type KeyType;
			internal Type ValueType;

			public MapKey(Type keyType, Type valueType)
			{
				KeyType = keyType;
				ValueType = valueType;
			}

			public bool Equals(MapKey other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				return Equals(other.KeyType, KeyType) && Equals(other.ValueType, ValueType);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != typeof(MapKey)) return false;
				return Equals((MapKey)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return ((KeyType != null ? KeyType.GetHashCode() : 0) * 397) ^ (ValueType != null ? ValueType.GetHashCode() : 0);
				}
			}
		}

		static readonly Dictionary<MapKey, WriteMapDelegate>
			CacheFns = new Dictionary<MapKey, WriteMapDelegate>();

		public static Action<TextWriter, object, WriteObjectDelegate, WriteObjectDelegate>
			GetWriteGenericDictionary(Type keyType, Type valueType)
		{
			WriteMapDelegate writeFn;
			lock (CacheFns)
			{
				var mapKey = new MapKey(keyType, valueType);
				if (!CacheFns.TryGetValue(mapKey, out writeFn))
				{
					var genericType = typeof(ToStringDictionaryMethods<,,>)
						.MakeGenericType(keyType, valueType, typeof(TSerializer));

					var mi = genericType.GetMethod("WriteIDictionary", BindingFlags.Static | BindingFlags.Public);
					writeFn = (WriteMapDelegate)Delegate.CreateDelegate(typeof(WriteMapDelegate), mi);
					CacheFns.Add(mapKey, writeFn);
				}
			}
			return writeFn.Invoke;
		}

		public static void WriteIDictionary(TextWriter writer, object oMap)
		{
			WriteObjectDelegate writeKeyFn = null;
			WriteObjectDelegate writeValueFn = null;

			writer.Write(JsWriter.MapStartChar);

			var map = (IDictionary)oMap;
			var ranOnce = false;
			foreach (var key in map.Keys)
			{
				var dictionaryValue = map[key];
				if (writeKeyFn == null)
					writeKeyFn = Serializer.GetWriteFn(key.GetType());

				if (writeValueFn == null)
					writeValueFn = Serializer.GetWriteFn(dictionaryValue.GetType());

				JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

				JsState.WritingKeyCount++;
				writeKeyFn(writer, key);
				JsState.WritingKeyCount--;

				writer.Write(JsWriter.MapKeySeperator);

				JsState.IsWritingValue = true;
				writeValueFn(writer, dictionaryValue ?? JsWriter.MapNullValue);
				JsState.IsWritingValue = false;
			}

			writer.Write(JsWriter.MapEndChar);
		}
	}

	internal static class ToStringDictionaryMethods<TKey, TValue, TSerializer>
		where TSerializer : ITypeSerializer
	{
		private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

		public static void WriteIDictionary(
			TextWriter writer,
			object oMap,
			WriteObjectDelegate writeKeyFn,
			WriteObjectDelegate writeValueFn)
		{
			WriteGenericIDictionary(writer, (IDictionary<TKey, TValue>)oMap, writeKeyFn, writeValueFn);
		}

		public static void WriteGenericIDictionary(
			TextWriter writer,
			IDictionary<TKey, TValue> map,
			WriteObjectDelegate writeKeyFn,
			WriteObjectDelegate writeValueFn)
		{
			writer.Write(JsWriter.MapStartChar);

			var ranOnce = false;
			foreach (var kvp in map)
			{
				JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

				JsState.WritingKeyCount++;
				writeKeyFn(writer, kvp.Key);
				JsState.WritingKeyCount--;

				writer.Write(JsWriter.MapKeySeperator);

				JsState.IsWritingValue = true;
				writeValueFn(writer, kvp.Value);
				JsState.IsWritingValue = false;
			}

			writer.Write(JsWriter.MapEndChar);
		}
	}
}