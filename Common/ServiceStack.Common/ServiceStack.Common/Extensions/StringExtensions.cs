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

		public static string SplitCamelCase(this string value)
		{
			return RegexSplitCamelCase.Replace(value, " $1").TrimStart();
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

		public static bool IsEmpty(this string value)
		{
			return string.IsNullOrEmpty(value);
		}

		public static bool IsNullOrEmpty(this string value)
		{
			return string.IsNullOrEmpty(value);
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
					// || charCode == 47					// /
					// || charCode == 38					// &
					// || charCode == 58					// :
					// || charCode == 61					// =
					// || charCode == 63					// ?
					// || charCode == 126					// ~
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

	}
}