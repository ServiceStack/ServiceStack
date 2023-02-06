using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.Redis
{
    public static class RedisDataInfoExtensions
    {
        public static string ToJsonInfo(this RedisText redisText)
        {
            var source = redisText.GetResult();
            return Parse(source);
        }

        private static string Parse(string source)
        {
            var result = new Dictionary<string, Dictionary<string, string>>();
            var section = new Dictionary<string, string>();

            var rows = SplitRows(source);

            foreach (var row in rows)
            {
                if (row.IndexOf("#", StringComparison.Ordinal) == 0)
                {
                    var name = ParseSection(row);
                    section = new Dictionary<string, string>();
                    result.Add(name, section);
                }
                else
                {
                    var pair = ParseKeyValue(row);
                    if (pair.HasValue)
                    {
                        section.Add(pair.Value.Key, pair.Value.Value);
                    }
                }
            }

            return JsonSerializer.SerializeToString(result);
        }

        private static string[] CRLF = { "\r\n" };
        private static IEnumerable<string> SplitRows(string source)
        {
            return source.Split(CRLF, StringSplitOptions.None).Where(n => !string.IsNullOrWhiteSpace(n));
        }

        private static string ParseSection(string source)
        {
            return (source.IndexOf("#", StringComparison.Ordinal) == 0)
                ? source.Trim('#').Trim()
                : string.Empty;
        }

        private static KeyValuePair<string, string>? ParseKeyValue(string source)
        {
            KeyValuePair<string, string>? result = null;

            var divider = source.IndexOf(":", StringComparison.Ordinal);
            if (divider > 0)
            {
                var name = source.Substring(0, divider);
                var value = source.Substring(divider + 1);
                result = new KeyValuePair<string, string>(name.Trim(), value.Trim());
            }

            return result;
        }
    }
}
