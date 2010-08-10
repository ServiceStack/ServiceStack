using System.IO;

namespace ServiceStack.Text.Json
{
	public static class JsonUtils
	{
		public static void WriteString(TextWriter writer, string value)
		{
			var hexSeqBuffer = new char[4];
			writer.Write('"');

			var len = value.Length;
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

			writer.Write('"');
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