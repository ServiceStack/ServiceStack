using System;
using System.Text.RegularExpressions;
using ServiceStack.Common.Utils;

namespace ServiceStack.Common.Extensions
{
	public static class StringExtensions
	{
		static readonly Regex regexSplitCamelCase = new Regex("([A-Z])", RegexOptions.Compiled);

		public static T ToEnum<T>(this string value)
		{
			return (T)Enum.Parse(typeof(T), value, true);
		}

		public static string SplitCamelCase(this string value)
		{
			return regexSplitCamelCase.Replace(value, " $1");
		}

		public static T To<T>(this string value)
		{
			return StringConverterUtils.Parse<T>(value);
		}

		public static T To<T>(this string value, T defaultValue)
		{
			return string.IsNullOrEmpty(value) ? defaultValue : StringConverterUtils.Parse<T>(value);
		}

		public static T ToOrDefaultValue<T>(this string value)
		{
			return string.IsNullOrEmpty(value) ? default(T) : StringConverterUtils.Parse<T>(value);
		}
	}
}