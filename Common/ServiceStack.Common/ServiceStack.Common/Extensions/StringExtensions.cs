using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using ServiceStack.Common.Utils;

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

		public static object To(this string value, Type type)
		{
			return StringConverterUtils.Parse(value, type);
		}

		public static bool IsEmpty(this string value)
		{
			return string.IsNullOrEmpty(value);
		}

		public static bool IsNullOrEmpty(this string value)
		{
			return string.IsNullOrEmpty(value);
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

		/// <summary>
		/// Converts from base: 0 - 62
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="from">From.</param>
		/// <param name="to">To.</param>
		/// <returns></returns>
		public static string BaseConvert(this string source, int from, int to)
		{
			const string chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
			var result = "";
			var length = source.Length;
			var number = new int[length];

			for (var i = 0; i < length; i++)
			{
				number[i] = chars.IndexOf(source[i]);
			}

			int newlen;

			do
			{
				var divide = 0;
				newlen = 0;

				for (var i = 0; i < length; i++)
				{
					divide = divide * from + number[i];

					if (divide >= to)
					{
						number[newlen++] = (int)(divide / to);
						divide = divide % to;
					}
					else if (newlen > 0)
					{
						number[newlen++] = 0;
					}
				}

				length = newlen;
				result = chars[divide] + result;
			}
			while (newlen != 0);

			return result;
		}

		public static bool ContainsAny(this string text, params string[] testMatches)
		{	
			foreach (var testMatch in testMatches)
			{
				if (text.Contains(testMatch)) return true;
			}
			return false;
		}

		public static string EncodeXml(this string value)
		{
			return value.Replace("<", "&lt;").Replace(">", "&gt;").Replace("&", "&amp;");
		}

		public static string UrlEncode(this string text)
		{
			if (string.IsNullOrEmpty(text)) return text;

			var tmp = new StringBuilder();

			for (var i=0; i < text.Length; i++)
			{
				var c = text.Substring(i, 1);
				int charCode = text[i];

				if (
					charCode >= 65 && charCode <= 90		// A-Z
					|| charCode >= 97 && charCode <= 122    // a-z
					|| charCode >= 48 && charCode <= 57		// 0-9
					|| charCode == 45						// - 
					|| charCode == 46						// .
					)
				{
					tmp.Append(c);
				}
				else
				{
					tmp.Append('%' + charCode.ToString("x"));
				}
			}
			return tmp.ToString();
		}

		public static string UrlDecode(this string text)
		{
			if (string.IsNullOrEmpty(text)) return null;

			var tmp = new StringBuilder();

			for (var i=0; i < text.Length; i++)
			{
				var c = text.Substring(i, 1);
				if (c == "+")
				{
					tmp.Append(" ");
				}
				else if (c == "%")
				{
					var hexNo = Convert.ToInt32(text.Substring(i + 1, 2), 16);
					tmp.Append((char)hexNo);
					i += 2;
				}
				else
				{
					tmp.Append(c);
				}
			}

			return tmp.ToString();
		}

		public static string UrlFormat(this string url, params string[] urlComponents)
		{
			var encodedUrlComponents = new List<string>(urlComponents).ConvertAll(x => x.UrlEncode());
			
			return string.Format(url, encodedUrlComponents.ToArray());
		}

		public static string Join(this List<string> items)
		{
			return string.Join(", ", items.ToArray());
		}

		public static string Join(this List<string> items, string delimeter)
		{
			return string.Join(delimeter, items.ToArray());
		}

		public static string ToRot13(this string value)
		{
			var array = value.ToCharArray();
			for (var i = 0; i < array.Length; i++)
			{
				var number = (int)array[i];

				if (number >= 'a' && number <= 'z')
					number += (number > 'm') ? -13 : 13;

				else if (number >= 'A' && number <= 'Z')
					number += (number > 'M') ? -13 : 13;

				array[i] = (char)number;
			}
			return new string(array);
		}
	}

}