//
// http://code.google.com/p/servicestack/wiki/TypeSerializer
// ServiceStack.Text: .NET C# POCO Type Text Serializer.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//	 Peter Townsend (townsend.pete@gmail.com)
//
// Copyright 2010 Liquidbit Ltd.
//
// Licensed under the same terms of ServiceStack: new BSD license.
//

using System;
using System.Text;

namespace ServiceStack.Text
{
	public static class JsvFormatter
	{
		public static string ToPrettyFormat<T>(this T poco)
		{
			var pocoStr = TypeSerializer.SerializeToString(poco);
			return Format(pocoStr);
		}

		public static string Format(string serializedText)
		{
			var tabCount = 0;
			var sb = new StringBuilder();

			for (var i = 0; i < serializedText.Length; i++)
			{
				var current = serializedText[i];

				if (current == TypeSerializer.MapStartChar || current == TypeSerializer.ListStartChar)
				{
					var previous = i - 1 >= 0 ? serializedText[i - 1] : 'a';
					if (previous == TypeSerializer.MapKeySeperator)
					{
						AppendTabLine(sb, tabCount);
					}

					sb.Append(current);
					AppendTabLine(sb, ++tabCount);
					continue;
				}

				if (current == TypeSerializer.MapEndChar || current == TypeSerializer.ListEndChar)
				{
					AppendTabLine(sb, --tabCount);
					sb.Append(current);
					continue;
				}

				if (current == TypeSerializer.ItemSeperator)
				{
					sb.Append(current);
					AppendTabLine(sb, tabCount);
					continue;
				}

				sb.Append(current);
			}

			return sb.ToString();
		}

		private static void AppendTabLine(StringBuilder sb, int tabCount)
		{
			sb.AppendLine();

			if (tabCount > 0)
			{
				sb.Append(new String('\t', tabCount));
			}
		}
	}
}