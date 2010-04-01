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
using System.Xml;

namespace ServiceStack.Text.Jsv
{
	public static class DeserializeBuiltin
	{
		public static Func<string, object> GetParseMethod(Type type)
		{
			if (type == typeof(string))
				return ParseUtils.ParseString;
			if (type == typeof(bool))
				return value => bool.Parse(value);
			if (type == typeof(byte))
				return value => byte.Parse(value);
			if (type == typeof(sbyte))
				return value => sbyte.Parse(value);
			if (type == typeof(short))
				return value => short.Parse(value);
			if (type == typeof(int))
				return value => int.Parse(value);
			if (type == typeof(long))
				return value => long.Parse(value);
			if (type == typeof(float))
				return value => float.Parse(value);
			if (type == typeof(double))
				return value => double.Parse(value);
			if (type == typeof(decimal))
				return value => decimal.Parse(value);

			if (type == typeof(Guid))
				return value => new Guid(value);
			if (type == typeof(DateTime))
				return value => DateTimeSerializer.ParseShortestXsdDateTime(value);
			if (type == typeof(TimeSpan))
				return value => TimeSpan.Parse(value);

			if (type == typeof(char))
				return value => char.Parse(value);
			if (type == typeof(ushort))
				return value => ushort.Parse(value);
			if (type == typeof(uint))
				return value => uint.Parse(value);
			if (type == typeof(ulong))
				return value => ulong.Parse(value);

			return null;
		}
	}


	public static class DeserializeBuiltin<T>
	{
		private static readonly Func<string, object> CachedParseFn;
		static DeserializeBuiltin()
		{
			CachedParseFn = GetParseFn();
		}

		public static Func<string, object> Parse
		{
			get { return CachedParseFn; }
		}

		private static Func<string, object> GetParseFn()
		{
			Type type = typeof(T);
			if (type == typeof(string))
				return ParseUtils.ParseString;
			if (type == typeof(bool) || type == typeof(bool?))
				return value => bool.Parse(value);
			if (type == typeof(byte) || type == typeof(byte?))
				return value => byte.Parse(value);
			if (type == typeof(sbyte) || type == typeof(sbyte?))
				return value => sbyte.Parse(value);
			if (type == typeof(short) || type == typeof(short?))
				return value => short.Parse(value);
			if (type == typeof(int) || type == typeof(int?))
				return value => int.Parse(value);
			if (type == typeof(long) || type == typeof(long?))
				return value => long.Parse(value);
			if (type == typeof(float) || type == typeof(float?))
				return value => float.Parse(value);
			if (type == typeof(double) || type == typeof(double?))
				return value => double.Parse(value);
			if (type == typeof(decimal) || type == typeof(decimal?))
				return value => decimal.Parse(value);

			if (type == typeof(Guid) || type == typeof(Guid?))
				return value => new Guid(value);
			if (type == typeof(DateTime) || type == typeof(DateTime?))
				return value => DateTimeSerializer.ParseShortestXsdDateTime(value);
			if (type == typeof(TimeSpan) || type == typeof(TimeSpan?))
				return value => TimeSpan.Parse(value);

			if (type == typeof(char) || type == typeof(char?))
				return value => char.Parse(value);
			if (type == typeof(ushort) || type == typeof(ushort?))
				return value => ushort.Parse(value);
			if (type == typeof(uint) || type == typeof(uint?))
				return value => uint.Parse(value);
			if (type == typeof(ulong) || type == typeof(ulong?))
				return value => ulong.Parse(value);

			return null;
		}


	}
}