using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ServiceStack.Text;
using ServiceStack.Text.Common;

namespace ServiceStack.Common.Extensions
{
	public static class StringExtensions
	{
		static readonly Regex RegexSplitCamelCase = new Regex("([A-Z]|[0-9]+)", RegexOptions.Compiled);

		public static T ToEnum<T>(this string value)
		{
			return (T)Enum.Parse(typeof(T), value, true);
		}

		public static T ToEnumOrDefault<T>(this string value, T defaultValue)
		{
			if (string.IsNullOrEmpty(value)) return defaultValue;
			return (T)Enum.Parse(typeof(T), value, true);
		}

		public static string SplitCamelCase(this string value)
		{
			return RegexSplitCamelCase.Replace(value, " $1").TrimStart();
		}

		public static string ToEnglish(this string camelCase)
		{
			var ucWords = camelCase.SplitCamelCase().ToLower();
			return ucWords[0].ToString().ToUpper() + ucWords.Substring(1);
		}

		public static bool IsEmpty(this string value)
		{
			return string.IsNullOrEmpty(value);
		}

		public static bool IsNullOrEmpty(this string value)
		{
			return string.IsNullOrEmpty(value);
		}

		public static bool EqualsIgnoreCase(this string value, string other)
		{
			return string.Equals(value, other, StringComparison.CurrentCultureIgnoreCase);
		}

		public static string ReplaceFirst(this string haystack, string needle, string replacement)
		{
			var pos = haystack.IndexOf(needle);
			if (pos < 0) return haystack;

			return haystack.Substring(0, pos) + replacement + haystack.Substring(pos + needle.Length);
		}

		public static string ReplaceAll(this string haystack, string needle, string replacement)
		{
			int pos;
			// Avoid a possible infinite loop
			if (needle == replacement) return haystack;
			while ((pos = haystack.IndexOf(needle)) > 0)
			{
				haystack = haystack.Substring(0, pos) 
					+ replacement 
					+ haystack.Substring(pos + needle.Length);
			}
			return haystack;
		}

		public static bool ContainsAny(this string text, params string[] testMatches)
		{	
			foreach (var testMatch in testMatches)
			{
				if (text.Contains(testMatch)) return true;
			}
			return false;
		}

		private static readonly Regex InvalidVarCharsRegEx = new Regex(@"[^A-Za-z0-9]", RegexOptions.Compiled);

		public static string SafeVarName(this string text)
		{
			if (string.IsNullOrEmpty(text)) return null;
			return InvalidVarCharsRegEx.Replace(text, "_");
		}

		public static string Join(this List<string> items)
		{
			return string.Join(JsWriter.ItemSeperatorString, items.ToArray());
		}

		public static string Join(this List<string> items, string delimeter)
		{
			return string.Join(delimeter, items.ToArray());
		}
		
	}

}