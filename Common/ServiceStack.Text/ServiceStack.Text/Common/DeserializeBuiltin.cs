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
using System.Globalization;

namespace ServiceStack.Text.Common
{
	public static class DeserializeBuiltin<T>
	{
		private static readonly ParseStringDelegate CachedParseFn;
		static DeserializeBuiltin()
		{
			CachedParseFn = GetParseFn();
		}

		public static ParseStringDelegate Parse
		{
			get { return CachedParseFn; }
		}

		private static ParseStringDelegate GetParseFn()
		{
			//Note the generic typeof(T) is faster than using var type = typeof(T)
			if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
				return value => bool.Parse(value);
			if (typeof(T) == typeof(byte) || typeof(T) == typeof(byte?))
				return value => byte.Parse(value);
			if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(sbyte?))
				return value => sbyte.Parse(value);
			if (typeof(T) == typeof(short) || typeof(T) == typeof(short?))
				return value => short.Parse(value);
			if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
				return value => int.Parse(value);
			if (typeof(T) == typeof(long) || typeof(T) == typeof(long?))
				return value => long.Parse(value);
			if (typeof(T) == typeof(float) || typeof(T) == typeof(float?))
				return value => float.Parse(value, CultureInfo.InvariantCulture);
			if (typeof(T) == typeof(double) || typeof(T) == typeof(double?))
				return value => double.Parse(value, CultureInfo.InvariantCulture);
			if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
				return value => decimal.Parse(value, CultureInfo.InvariantCulture);

			if (typeof(T) == typeof(Guid) || typeof(T) == typeof(Guid?))
				return value => new Guid(value);
			if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
				return value => DateTimeSerializer.ParseShortestXsdDateTime(value);
			if (typeof(T) == typeof(TimeSpan) || typeof(T) == typeof(TimeSpan?))
				return value => TimeSpan.Parse(value);

			if (typeof(T) == typeof(char) || typeof(T) == typeof(char?))
			{
				char cValue;
				return value => char.TryParse(value, out cValue) ? cValue : '\0';
			}
			if (typeof(T) == typeof(ushort) || typeof(T) == typeof(ushort?))
				return value => ushort.Parse(value);
			if (typeof(T) == typeof(uint) || typeof(T) == typeof(uint?))
				return value => uint.Parse(value);
			if (typeof(T) == typeof(ulong) || typeof(T) == typeof(ulong?))
				return value => ulong.Parse(value);

			return null;
		}
	}
}