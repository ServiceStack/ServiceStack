using System.Security.Cryptography;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    public static class SocialExtensions
    {
        public static string ToGravatarUrl(this string email, int size = 64)
        {
            var md5 = MD5.Create();
            var md5HashBytes = md5.ComputeHash(email.ToUtf8Bytes());

            var sb = StringBuilderCache.Allocate();
            foreach (var b in md5HashBytes)
            {
                sb.Append(b.ToString("x2"));
            }

            string gravatarUrl = $"https://www.gravatar.com/avatar/{StringBuilderCache.ReturnAndFree(sb)}?d=mm&s={size}";
            return gravatarUrl;
        }
    }
}
