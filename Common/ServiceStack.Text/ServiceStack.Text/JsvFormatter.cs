//
// http://code.google.com/p/servicestack/wiki/TypeSerializer
// ServiceStack.Text: .NET C# POCO Type Text Serializer.
//
// Authors:
//	 Peter Townsend (townsend.pete@gmail.com)
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2010 Liquidbit Ltd.
//
// Licensed under the same terms of ServiceStack: new BSD license.
//

using System;
using System.Text;
using ServiceStack.Text.Common;

namespace ServiceStack.Text
{
	public static class JsvFormatter
	{
		public static string Dump<T>(this T instance)
		{
			return SerializeAndFormat(instance);
		}

		public static string SerializeAndFormat<T>(this T instance)
		{
			var dtoStr = TypeSerializer.SerializeToString(instance);
			var formatStr = Format(dtoStr);
			return formatStr;
		}

		public static string Format(string serializedText)
		{
			if (string.IsNullOrEmpty(serializedText)) return null;

			var tabCount = 0;
			var sb = new StringBuilder();
			var firstKeySeparator = true;

			for (var i = 0; i < serializedText.Length; i++)
			{
				var current = serializedText[i];
				var previous = i - 1 >= 0 ? serializedText[i - 1] : 0;
				var next = i < serializedText.Length - 1 ? serializedText[i + 1] : 0;

				if (current == JsWriter.MapStartChar || current == JsWriter.ListStartChar)
				{
					if (previous == JsWriter.MapKeySeperator)
					{
						if (next == JsWriter.MapEndChar || next == JsWriter.ListEndChar)
						{
							sb.Append(current);
							sb.Append(serializedText[++i]); //eat next
							continue;
						}

						AppendTabLine(sb, tabCount);
					}

					sb.Append(current);
					AppendTabLine(sb, ++tabCount);
					firstKeySeparator = true;
					continue;
				}

				if (current == JsWriter.MapEndChar || current == JsWriter.ListEndChar)
				{
					AppendTabLine(sb, --tabCount);
					sb.Append(current);
					firstKeySeparator = true;
					continue;
				}

				if (current == JsWriter.ItemSeperator)
				{
					sb.Append(current);
					AppendTabLine(sb, tabCount);
					firstKeySeparator = true;
					continue;
				}

				sb.Append(current);

				if (current == JsWriter.MapKeySeperator && firstKeySeparator)
				{
					sb.Append(" ");
					firstKeySeparator = false;
				}
			}

			return sb.ToString();
		}

		private static void AppendTabLine(StringBuilder sb, int tabCount)
		{
			sb.AppendLine();

			if (tabCount > 0)
			{
				sb.Append(new string('\t', tabCount));
			}
		}
	}
}