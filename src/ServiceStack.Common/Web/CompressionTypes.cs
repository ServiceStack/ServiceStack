using System;

namespace ServiceStack.Common.Web
{
    public static class CompressionTypes
    {
        public static readonly string[] AllCompressionTypes = new[] { Deflate, GZip };

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
                case GZip:
                    return "." + compressionType;
                default:
                    throw new NotSupportedException(
                        "Unknown compressionType: " + compressionType);
            }
        }
    }
}