using System;
using System.Security.Cryptography;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.Aws.Support
{
    public static class HashExtensions
    {
        public static string ToSha256HashString64(this string toHash, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(toHash))
            {
                return string.Empty;
            }
            if (encoding == null)
            {
                encoding = Encoding.Unicode;
            }

            var bytes = encoding.GetBytes(toHash).ToSha256HashBytes();
            return ToBase64String(bytes);
        }

        public static string ToBase64String(this byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }

        public static string ToSha256Hash(this string value)
        {
            var sb = StringBuilderCache.Allocate();
            using (var hash = SHA256.Create())
            {
                var result = hash.ComputeHash(value.ToUtf8Bytes());
                foreach (var b in result)
                {
                    sb.Append(b.ToString("x2"));
                }
            }
            return StringBuilderCache.ReturnAndFree(sb);
        }

        public static byte[] ToSha256HashBytes(this byte[] bytes)
        {
            using (var hash = SHA256.Create())
            {
                return hash.ComputeHash(bytes);
            }
        }
    }
}
