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
            foreach (var b in md5HadhBytes)
            {
                sb.Append(b.ToString("x2"));
            }

            string gravatarUrl = $"http://www.gravatar.com/avatar/{StringBuilderCache.ReturnAndFree(sb)}?d=mm&s={size}";
            return gravatarUrl;
        }
    }
}