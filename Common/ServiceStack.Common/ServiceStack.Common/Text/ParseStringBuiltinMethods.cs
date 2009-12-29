using System;

namespace ServiceStack.Common.Text
{
	public static class ParseStringBuiltinMethods
	{
		public static Func<string, object> GetParseMethod(Type type)
		{
			if (type == typeof(string))
				return ParseStringMethods.ParseString;
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
				return value => DateTime.Parse(value);
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
}