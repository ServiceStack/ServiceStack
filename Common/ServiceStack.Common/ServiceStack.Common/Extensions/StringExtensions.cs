using System;
using System.Text.RegularExpressions;
using ServiceStack.Common.Utils;

namespace ServiceStack.Common.Extensions
{
	public static class StringExtensions
	{
		static readonly Regex RegexSplitCamelCase = new Regex("([A-Z])", RegexOptions.Compiled);

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

		public static bool IsNullOrEmpty(this string value)
		{
			return string.IsNullOrEmpty(value);
		}

		public static string UrlDecode(this string value)
		{
			return value;
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

		public static string EncodeXml(this string value)
		{
			return value.Replace("<", "&lt;").Replace(">", "&gt;").Replace("&", "&amp;");
		}

		public static string UrlEncode(this string text)
		{
			if (string.IsNullOrEmpty(text)) return text;

			string tmp;
			string c;

			for (var i=1; i<text.Length; i++)
			{
				c = text.Substring(i, 1);
				int charCode = text[i];
			}
			return text;
		}

		/*
			//If Len(s) = 0 Then Exit Function
	
			//Dim tmp As String
			//Dim c As String
			//Dim i As Integer
	
			//For i = 1 To Len(s)
			//    c = Mid(s, i, 1)
			//    If (Asc(c) &gt;= 65 And Asc(c) &lt;= 90) _
			//    Or (Asc(c) &gt;= 97 And Asc(c) &lt;= 122) _
			//    Or (Asc(c) &gt;= 48 And Asc(c) &lt;= 58) _
			//    Or Asc(c) = 38 _
			//    Or (Asc(c) &gt;= 45 And Asc(c) &lt;= 47) _
			//    Or Asc(c) = 58 Or Asc(c) = 61 _
			//    Or Asc(c) = 63 Or Asc(c) = 126 Then
			//        tmp = tmp + c
			//    Else
			//        tmp = tmp + "%" + Hex(Asc(c))
			//    End If
			//Next i
			//urlEncode = tmp		
		 * 
		 * Public Function urlDecode(s As String) As String
			If Len(s) = 0 Then Exit Function
			Dim i As Integer
			Dim tmp As String
			Dim c As String
			For i = 1 To Len(s)
				c = Mid$(s, i, 1)
				If c = "+" Then c = " "
				If c = "%" Then
					c = Chr$("&H" + Mid$(s, i + 1, 2))
					i = i + 2
				End If
				tmp = tmp + c
			Next i
			urlDecode = tmp
		End Function
 
		Public Function urlEncode(s As String) As String

		End Function */

	}
}