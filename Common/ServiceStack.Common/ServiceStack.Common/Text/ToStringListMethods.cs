using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Common.Text
{
	public static class ToStringListMethods
	{
		public static Func<object, string> GetToStringMethod<T>()
		{
			var type = typeof(T);

			return GetToStringMethod(type);
		}

		public static Func<object, string> GetToStringMethod(Type type)
		{
			var listInterfaces = type.FindInterfaces(
				(t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>), null);

			if (listInterfaces.Length == 0)
				throw new ArgumentException(string.Format("Type {0} is not of type IList<>", type.FullName));


			//optimized access for regularly used types
			if (type == typeof(IList<string>))
				return value => IListStringToString((IList<string>)value);

			if (type == typeof(IList<int>))
				return ValueTypeToString<int>;


			var elementType = listInterfaces[0].GetGenericArguments()[0];

			if (elementType.IsValueType)
			{
				return GetValueTypeListToStringMethod(elementType);
			}

			var toStringFn = ToStringMethods.GetToStringMethod(elementType);
			var toStringDelegate = GetGenericListToStringMethod(elementType);

			return value => toStringDelegate(value, toStringFn);
		}

		private static Func<object, string> GetValueTypeListToStringMethod(Type elementType)
		{
			var mi = typeof(ToStringListMethods).GetMethod("ValueTypeToString", BindingFlags.Static | BindingFlags.Public);
			ToStringDelegate valueTypeToStringDelegate;
			if (!ValueTypeToStringDelegateCache.TryGetValue(elementType, out valueTypeToStringDelegate))
			{
				var genericMi = mi.MakeGenericMethod(new[] { elementType });
				valueTypeToStringDelegate = (ToStringDelegate)Delegate.CreateDelegate(typeof(ToStringDelegate), genericMi);
				ValueTypeToStringDelegateCache[elementType] = valueTypeToStringDelegate;
			}

			return valueTypeToStringDelegate.Invoke;
		}

		private static CollectionToStringDelegate GetGenericListToStringMethod(Type elementType)
		{
			var mi = typeof(ToStringListMethods).GetMethod("GenericIListToString", BindingFlags.Static | BindingFlags.Public);
			CollectionToStringDelegate toStringDelegate;
			if (!GenericToStringDelegateCache.TryGetValue(elementType, out toStringDelegate))
			{
				var genericMi = mi.MakeGenericMethod(new[] { elementType });
				toStringDelegate = (CollectionToStringDelegate)Delegate.CreateDelegate(typeof(CollectionToStringDelegate), genericMi);
				GenericToStringDelegateCache[elementType] = toStringDelegate;
			}

			return toStringDelegate;
		}

		private static readonly Dictionary<Type, CollectionToStringDelegate> GenericToStringDelegateCache 
			= new Dictionary<Type, CollectionToStringDelegate>();

		private static readonly Dictionary<Type, ToStringDelegate> ValueTypeToStringDelegateCache 
			= new Dictionary<Type, ToStringDelegate>();

		public static string IListToString(IList list)
		{
			Func<object, string> toStringFn = null;

			var sb = new StringBuilder();
			var listLength = list.Count;
			for (var i=0; i < listLength; i++)
			{
				var item = list[i];
				if (toStringFn == null)
				{
					toStringFn = GetToStringMethod(item.GetType());
				}

				var itemString = toStringFn(item);
				if (sb.Length > 0)
				{
					sb.Append(TextExtensions.ItemSeperator);
				}
				sb.Append(itemString);
			}
			sb.Insert(0, TextExtensions.ListStartChar);
			sb.Append(TextExtensions.ListEndChar);

			return sb.ToString();
		}

		public static string StringArrayToString(string[] arrayValue)
		{
			var sb = new StringBuilder();
			var arrayValueLength = arrayValue.Length;
			for (var i=0; i < arrayValueLength; i++)
			{
				if (sb.Length > 0) sb.Append(TextExtensions.ItemSeperator);
				sb.Append(arrayValue[i].ToCsvField());
			}
			sb.Insert(0, TextExtensions.ListStartChar);
			sb.Append(TextExtensions.ListEndChar);
			return sb.ToString();
		}

		public static Func<object, string> GetArrayToStringMethod(Type elementType)
		{
			var mi = typeof(ToStringListMethods).GetMethod("ArrayToString",
				BindingFlags.Static | BindingFlags.Public);

			var genericMi = mi.MakeGenericMethod(new[] { elementType });
			var genericDelegate = (CollectionToStringDelegate)Delegate.CreateDelegate(typeof(CollectionToStringDelegate), genericMi);

			var toStringFn = ToStringMethods.GetToStringMethod(elementType);
			return value => genericDelegate(value, toStringFn);
		}

		public static string ArrayToString<T>(object oArrayValue, Func<object, string> toStringFn)
		{
			var arrayValue = (T[])oArrayValue;
			var sb = new StringBuilder();
			var arrayValueLength = arrayValue.Length;
			for (var i=0; i < arrayValueLength; i++)
			{
				var item = arrayValue[i];

				var itemString = toStringFn(item);
				if (sb.Length > 0)
				{
					sb.Append(TextExtensions.ItemSeperator);
				}
				sb.Append(itemString);
			}
			sb.Insert(0, TextExtensions.ListStartChar);
			sb.Append(TextExtensions.ListEndChar);
			return sb.ToString();
		}

		public static string IListStringToString(IList<string> list)
		{
			var sb = new StringBuilder();
			var listCount = list.Count;
			for (var i=0; i < listCount; i++)
			{
				if (sb.Length > 0)
				{
					sb.Append(TextExtensions.ItemSeperator);
				}
				sb.Append(ToStringMethods.BuiltinToString(list[i]));
			}
			sb.Insert(0, TextExtensions.ListStartChar);
			sb.Append(TextExtensions.ListEndChar);
			return sb.ToString();
		}

		public static string ValueTypeToString<T>(object oList)
		{
			var list = (IList<T>)oList;
			var sb = new StringBuilder();
			var listCount = list.Count;
			for (var i=0; i < listCount; i++)
			{
				if (sb.Length > 0)
				{
					sb.Append(TextExtensions.ItemSeperator);
				}
				sb.Append(list[i]);
			}
			sb.Insert(0, TextExtensions.ListStartChar);
			sb.Append(TextExtensions.ListEndChar);
			return sb.ToString();
		}

		public static string GenericIListToString<T>(object oList, Func<object, string> toStringFn)
		{
			var list = (IList<T>)oList;
			var sb = new StringBuilder();
			var listCount = list.Count;
			for (var i=0; i < listCount; i++)
			{
				if (sb.Length > 0)
				{
					sb.Append(TextExtensions.ItemSeperator);
				}
				var value = toStringFn(list[i]);
				sb.Append(value);
			}
			sb.Insert(0, TextExtensions.ListStartChar);
			sb.Append(TextExtensions.ListEndChar);
			return sb.ToString();
		}
	}
}