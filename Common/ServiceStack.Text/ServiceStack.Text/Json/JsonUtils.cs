using System.IO;

namespace ServiceStack.Text.Json
{
	public static class JsonUtils
	{
		public const char EscapeChar = '\\';
		public const char QuoteChar = '"';
		public const string Null = "null";
		public const string True = "true";
		public const string False = "false";

		static readonly char[] EscapeChars = new[]
			{
				QuoteChar, '\n', '\r', '\t', '"', '\\', '\f', '\b',
			};

		private const int LengthFromLargestChar = '\\' + 1;
		private static readonly bool[] HasEscapeChars = new bool[LengthFromLargestChar];

		static JsonUtils()
		{
			foreach (var escapeChar in EscapeChars)
			{
				HasEscapeChars[escapeChar] = true;
			}
		}

		public static void WriteString(TextWriter writer, string value)
		{
			var len = value.Length;

			//micro optimizations: instead of value.IndexOfAny(EscapeChars)
			var hasEscapeChars = false;
			for (var i = 0; i < len; i++)
			{
				var c = value[i];
				if (c >= LengthFromLargestChar || !HasEscapeChars[c]) continue;
				hasEscapeChars = true;
				break;
			}
			if (!hasEscapeChars)
			{
				writer.Write(QuoteChar);
				writer.Write(value);
				writer.Write(QuoteChar);
				return;
			}

			var hexSeqBuffer = new char[4];
			writer.Write(QuoteChar);

			for (var i = 0; i < len; i++)
			{
				switch (value[i])
				{
					case '\n':
						writer.Write("\\n");
						continue;

					case '\r':
						writer.Write("\\r");
						continue;

					case '\t':
						writer.Write("\\t");
						continue;

					case '"':
					case '\\':
						writer.Write('\\');
						writer.Write(value[i]);
						continue;

					case '\f':
						writer.Write("\\f");
						continue;

					case '\b':
						writer.Write("\\b");
						continue;
				}

				//Is printable char?
				if (value[i] >= 32 && value[i] <= 126)
				{
					writer.Write(value[i]);
					continue;
				}

				// Default, turn into a \uXXXX sequence
				IntToHex(value[i], hexSeqBuffer);
				writer.Write("\\u");
				writer.Write(hexSeqBuffer);
			}

			writer.Write(QuoteChar);
		}

		public static void IntToHex(int intValue, char[] hex)
		{
			for (var i = 0; i < 4; i++)
			{
				var num = intValue % 16;

				if (num < 10)
					hex[3 - i] = (char)('0' + num);
				else
					hex[3 - i] = (char)('A' + (num - 10));

				intValue >>= 4;
			}
		}
	}

}