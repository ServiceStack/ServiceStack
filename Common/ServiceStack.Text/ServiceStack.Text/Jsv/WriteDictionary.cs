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
	public delegate void WriteMapDelegate(
		TextWriter writer,
		object oMap,
		Action<TextWriter, object> writeKeyFn,
		Action<TextWriter, object> writeValueFn);

	public static class WriteDictionary
	{
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

		public static Action<TextWriter, object, Action<TextWriter, object>, Action<TextWriter, object>>
			GetWriteGenericDictionary(Type keyType, Type valueType)
		{
			WriteMapDelegate writeFn;
			lock (CacheFns)
			{
				var mapKey = new MapKey(keyType, valueType);
				if (!CacheFns.TryGetValue(mapKey, out writeFn))
				{
					var genericType = typeof(ToStringDictionaryMethods<,>).MakeGenericType(keyType, valueType);
					var mi = genericType.GetMethod("WriteIDictionary", BindingFlags.Static | BindingFlags.Public);
					writeFn = (WriteMapDelegate)Delegate.CreateDelegate(typeof(WriteMapDelegate), mi);
					CacheFns.Add(mapKey, writeFn);
				}
			}
			return writeFn.Invoke;
		}

		public static void WriteIDictionary(TextWriter writer, object oMap)
		{
			Action<TextWriter, object> writeKeyFn = null;
			Action<TextWriter, object> writeValueFn = null;

			writer.Write(TypeSerializer.MapStartChar);

			var map = (IDictionary)oMap;
			var ranOnce = false;
			foreach (var key in map.Keys)
			{
				var dictionaryValue = map[key];
				if (writeKeyFn == null)
					writeKeyFn = JsvWriter.GetWriteFn(key.GetType());

				if (writeValueFn == null)
					writeValueFn = JsvWriter.GetWriteFn(dictionaryValue.GetType());


				TypeSerializer.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

				writeKeyFn(writer, key);

				writer.Write(TypeSerializer.MapKeySeperator);

				writeValueFn(writer, dictionaryValue ?? TypeSerializer.MapNullValue);
			}

			writer.Write(TypeSerializer.MapEndChar);
		}
	}

	public static class ToStringDictionaryMethods<K, V>
	{
		public static void WriteIDictionary(
			TextWriter writer,
			object oMap,
			Action<TextWriter, object> writeKeyFn,
			Action<TextWriter, object> writeValueFn)
		{
			WriteGenericIDictionary(writer, (IDictionary<K, V>)oMap, writeKeyFn, writeValueFn);
		}

		public static void WriteGenericIDictionary(
			TextWriter writer,
			IDictionary<K, V> map,
			Action<TextWriter, object> writeKeyFn,
			Action<TextWriter, object> writeValueFn)
		{
			writer.Write(TypeSerializer.MapStartChar);

			var ranOnce = false;
			foreach (var kvp in map)
			{
				TypeSerializer.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

				writeKeyFn(writer, kvp.Key);

				writer.Write(TypeSerializer.MapKeySeperator);

				writeValueFn(writer, kvp.Value);
			}

			writer.Write(TypeSerializer.MapEndChar);
		}
	}
}