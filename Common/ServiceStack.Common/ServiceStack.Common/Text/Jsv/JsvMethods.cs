using System.IO;

namespace ServiceStack.Common.Text.Jsv
{
	public static class JsvMethods
	{
		public static void WriteBuiltIn(TextWriter writer, object value)
		{
			if (value == null) return;
			writer.Write(value);
		}

		public static void WriteItemSeperatorIfRanOnce(TextWriter writer, ref bool ranOnce)
		{
			if (ranOnce)
				writer.Write(TypeSerializer.ItemSeperator);
			else
				ranOnce = true;
		}
		
	}
}