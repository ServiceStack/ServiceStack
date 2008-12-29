using System;
using System.Text.RegularExpressions;

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
	}
}