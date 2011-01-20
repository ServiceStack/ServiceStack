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
using System.IO;
using System.Reflection;
using System.Text;

namespace ServiceStack.Text
{
	/// <summary>
	/// Mini version of QueryStringSerializer for MonoTouch
	/// </summary>
	public static class QueryStringSerializer
	{
		public static string SerializeToString<T>(T value)
		{
			var sb = new StringBuilder(4096);
			using (var writer = new StringWriter(sb, CultureInfo.InvariantCulture))
			{
				WriteQueryString(writer, value);
			}
			return sb.ToString();
		}

		public static void WriteQueryString(TextWriter writer, object value)
		{
			var i = 0;
			foreach (var propertyInfo in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				var mi = propertyInfo.GetGetMethod();
				if (mi == null) continue;
				var propertyValue = mi.Invoke(value, new object[0]);
				if (propertyValue == null) continue;
				var propertyValueString = propertyValue as string;
				if (propertyValueString != null)
				{
					propertyValue = propertyValueString.UrlEncode();
				}

				if (i++ > 0)
					writer.Write('&');

				WritePropertyName(writer, propertyInfo.Name);
				writer.Write('=');
				writer.Write(propertyValue);
			}
		}

		public static void WritePropertyName(TextWriter writer, string value)
		{
			writer.Write(value);
		}

		public static string UrlEncode(this string text)
		{
			if (string.IsNullOrEmpty(text)) return text;

			var sb = new StringBuilder();

			var textLength = text.Length;
			for (var i = 0; i < textLength; i++)
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
					sb.Append(c);
				}
				else
				{
					sb.Append('%' + charCode.ToString("x"));
				}
			}
			return sb.ToString();
		}

		public static string WithTrailingSlash(this string path)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");

			if (path[path.Length - 1] != '/')
			{
				return path + "/";
			}
			return path;
		}
	
		public static byte[] ToUtf8Bytes(this string value)
		{
			return Encoding.UTF8.GetBytes(value);
		}
	}
	
}