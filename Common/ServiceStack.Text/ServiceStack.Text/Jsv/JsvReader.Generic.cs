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
using System.Collections.Generic;
using System.Reflection;
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Jsv
{
	public static class JsvReader
	{ 
		internal static readonly JsReader<JsvTypeSerializer> Instance = new JsReader<JsvTypeSerializer>();

		private static readonly Dictionary<Type, ParseFactoryDelegate> ParseFnCache =
			new Dictionary<Type, ParseFactoryDelegate>();

		public static ParseStringDelegate GetParseFn(Type type)
		{
			ParseFactoryDelegate parseFactoryFn;
			lock (ParseFnCache)
			{
				if (!ParseFnCache.TryGetValue(type, out parseFactoryFn))
				{
					var genericType = typeof(JsvReader<>).MakeGenericType(type);
					var mi = genericType.GetMethod("GetParseFn",
						BindingFlags.Public | BindingFlags.Static);

					parseFactoryFn = (ParseFactoryDelegate)Delegate.CreateDelegate(
						typeof(ParseFactoryDelegate), mi);

					ParseFnCache.Add(type, parseFactoryFn);					
				}
			}
			return parseFactoryFn();
		}
	}

	public static class JsvReader<T>
	{
		private static readonly ParseStringDelegate ReadFn;

		static JsvReader()
		{
			ReadFn = JsvReader.Instance.GetParseFn<T>();
		}
		
		public static ParseStringDelegate GetParseFn()
		{
			return ReadFn;
		}

		public static object Parse(string value)
		{
			if (ReadFn == null)
			{
				if (typeof(T).IsInterface)
					throw new NotSupportedException("Can not deserialize interface type: "
						+ typeof(T).Name);
			}
			return value == null 
			       	? null 
			       	: ReadFn(value);
		}
	}
}