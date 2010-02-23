using System;

namespace ServiceStack.Common.Web
{
	public static class CompressionTypes
	{
		public const string Default = Deflate;
		public const string Deflate = "deflate";
		public const string GZip = "gzip";

		public static bool IsValid(string compressionType)
		{
			return compressionType == Deflate || compressionType == GZip;
		}

		public static void AssertIsValid(string compressionType)
		{
			if (!IsValid(compressionType))
			{
				throw new NotSupportedException(compressionType 
					+ " is not a supported compression type. Valid types: gzip, deflate.");
			}
		}

		public static string GetExtension(string compressionType)
		{
			switch (compressionType)
			{
				case Deflate:
					return ".7z";
				case GZip:
					return ".gz";
				default:
					throw new NotSupportedException(
						"Unknown compressionType: " + compressionType);
			}
		}
	}
}