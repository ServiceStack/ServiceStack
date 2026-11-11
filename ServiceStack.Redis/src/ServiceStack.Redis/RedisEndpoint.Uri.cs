using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Authentication;

namespace ServiceStack.Redis;

public partial class RedisEndpoint
{
    /// <summary>
    /// Opt-in URI semantics: user:password identifies ACL credentials, /db selects the database.
    /// rediss enables TLS (default port 6380); redis defaults to 6379. Legacy redis:// parsing is unchanged.
    /// </summary>
    public static RedisEndpoint ParseUri(string value, int? defaultPort = null)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        try
        {
            _ = Decode(value); // Reject malformed escaping before System.Uri canonicalizes it.
            if (value != value.Trim()) throw new FormatException();
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "redis" && uri.Scheme != "rediss") || uri.Host.Length == 0 || uri.Fragment.Length != 0)
                throw new FormatException();
            var ssl = uri.Scheme == "rediss";
            var endpoint = new RedisEndpoint(uri.Host.Trim('[', ']'), uri.Port < 0
                ? defaultPort ?? (ssl ? RedisConfig.DefaultPortSsl : RedisConfig.DefaultPort)
                : uri.Port) { Ssl = ssl };
            if (endpoint.Port is < 1 or > 65535) throw new FormatException();
            var userInfo = uri.GetComponents(UriComponents.UserInfo, UriFormat.UriEscaped);
            if (userInfo.Length != 0)
            {
                var colon = userInfo.IndexOf(':');
                if (colon < 0) throw new FormatException();
                endpoint.Username = colon == 0 ? null : Decode(userInfo.Substring(0, colon));
                endpoint.Password = Decode(userInfo.Substring(colon + 1));
            }

            var path = uri.GetComponents(UriComponents.Path, UriFormat.UriEscaped);
            if (path.Length != 0)
            {
                if (!long.TryParse(Decode(path), NumberStyles.None, CultureInfo.InvariantCulture, out var db) || db < 0)
                    throw new FormatException();
                endpoint.Db = db;
            }

            // Reject ambiguous path segments even if System.Uri would normalize them away.
            var authorityEnd = value.IndexOf('/', value.IndexOf("://", StringComparison.Ordinal) + 3);
            var queryStart = value.IndexOf('?');
            if (queryStart >= 0 && authorityEnd > queryStart) authorityEnd = -1;
            if (authorityEnd >= 0)
            {
                var rawPath = value.Substring(authorityEnd).Split('?', '#')[0];
                if (rawPath.IndexOf('/', 1) >= 0) throw new FormatException();
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in uri.Query.TrimStart('?').Split('&'))
            {
                if (item.Length == 0) continue;
                var pair = item.Split(['='], 2);
                if (pair.Length != 2) throw new FormatException();
                var name = Decode(pair[0]);
                var option = Decode(pair[1]);
                // Accept the legacy parser's idletimeout alias; both spellings name one option.
                var key = name.ToLowerInvariant() == "idletimeout" ? "idletimeoutsecs" : name.ToLowerInvariant();
                if (!seen.Add(key)) throw new FormatException();
                switch (key)
                {
                    case "client": endpoint.Client = option; break;
                    case "namespaceprefix": endpoint.NamespacePrefix = option; break;
                    case "connecttimeout": endpoint.ConnectTimeout = Nonnegative(option); break;
                    case "sendtimeout": endpoint.SendTimeout = Nonnegative(option); break;
                    case "receivetimeout": endpoint.ReceiveTimeout = Nonnegative(option); break;
                    case "retrytimeout": endpoint.RetryTimeout = Nonnegative(option); break;
                    case "idletimeoutsecs": endpoint.IdleTimeOutSecs = Nonnegative(option); break;
                    case "sslprotocols":
                        endpoint.SslProtocols =
                            (SslProtocols)Enum.Parse(typeof(SslProtocols), option.Replace('|', ','), true); break;
                    default:
                        throw
                            new FormatException(); // Auth/DB/SSL belong to the URI, never conflicting query overrides.
                }
            }

            return endpoint;
        }
        catch (Exception e) when (e is FormatException || e is ArgumentException || e is OverflowException)
        {
            // Neither the URI nor parser exceptions may disclose credentials.
            throw new ArgumentException("Invalid Redis URI or unsupported/conflicting URI option.", nameof(value));
        }

        static int Nonnegative(string text)
        {
            var n = int.Parse(text, NumberStyles.None, CultureInfo.InvariantCulture);
            if (n < 0) throw new FormatException();
            return n;
        }

        static string Decode(string text)
        {
            for (var i = 0; i < text.Length; i++)
                if (text[i] == '%' &&
                    (i + 2 >= text.Length || !Uri.IsHexDigit(text[i + 1]) || !Uri.IsHexDigit(text[i + 2])))
                    throw new FormatException();
            return Uri.UnescapeDataString(text);
        }
    }
}