using System.Security.Cryptography;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    public static class SocialExtensions
    {
        public static string ToGravatarUrl(this string email, int size = 64)
        {
            var md5 = MD5.Create();
            var md5HadhBytes = md5.ComputeHash(email.ToUtf8Bytes());

            var sb = StringBuilderCache.Allocate();
            for (var i = 0; i < md5HadhBytes.Length; i++)
                sb.Append(md5HadhBytes[i].ToString("x2"));

            string gravatarUrl = "http://www.gravatar.com/avatar/{0}?d=mm&s={1}".Fmt(
                StringBuilderCache.ReturnAndFree(sb), size);
            return gravatarUrl;
        }
    }
}