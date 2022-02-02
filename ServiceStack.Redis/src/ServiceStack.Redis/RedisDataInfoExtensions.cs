using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.Redis
{
    public static class RedisDataInfoExtensions
    {
        public static String ToJsonInfo(this RedisText redisText)
        {
            var source = redisText.GetResult();
            return Parse(source);
        }

        #region Private

        private static String Parse(String source)
        {
            var result = new Dictionary<String, Dictionary<String, String>>();
            var section = new Dictionary<String, String>();

            var rows = SplitRows(source);

            foreach (var row in rows)
            {
                if (row.IndexOf("#", StringComparison.Ordinal) == 0)
                {
                    var name = ParseSection(row);
                    section = new Dictionary<String, String>();
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

        private static IEnumerable<String> SplitRows(String source)
        {
            return source.Split(new[] { "\r\n" }, StringSplitOptions.None).Where(n => !String.IsNullOrWhiteSpace(n));
        }

        private static String ParseSection(String source)
        {
            return (source.IndexOf("#", StringComparison.Ordinal) == 0)
                ? source.Trim('#').Trim()
                : String.Empty;
        }

        private static KeyValuePair<String, String>? ParseKeyValue(String source)
        {
            KeyValuePair<String, String>? result = null;

            var devider = source.IndexOf(":", StringComparison.Ordinal);
            if (devider > 0)
            {
                var name = source.Substring(0, devider);
                var value = source.Substring(devider + 1);
                result = new KeyValuePair<String, String>(name.Trim(), value.Trim());
            }

            return result;
        }

        #endregion Private
    }
}
