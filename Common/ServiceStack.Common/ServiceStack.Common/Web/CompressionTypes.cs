namespace ServiceStack.Common.Web
{
	public static class CompressionTypes
	{
		public const string Deflate = "deflate";
		public const string GZip = "gzip";

		public static bool IsValid(string compressionType)
		{
			return compressionType == Deflate || compressionType == GZip;
		}
	}
}