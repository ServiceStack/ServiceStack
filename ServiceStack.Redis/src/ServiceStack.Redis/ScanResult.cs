using System.Collections.Generic;
using System.Globalization;

namespace ServiceStack.Redis
{
    public static class ScanResultExtensions
    {
        public static List<string> AsStrings(this ScanResult result)
        {
            return result.Results.Map(x => x.FromUtf8Bytes());
        }

        public static Dictionary<string, double> AsItemsWithScores(this ScanResult result)
        {
            var to = new Dictionary<string, double>();
            for (var i = 0; i < result.Results.Count; i += 2)
            {
                var key = result.Results[i];
                var score = double.Parse(result.Results[i + 1].FromUtf8Bytes(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture);
                to[key.FromUtf8Bytes()] = score;
            }
            return to;
        }

        public static Dictionary<string, string> AsKeyValues(this ScanResult result)
        {
            var to = new Dictionary<string, string>();
            for (var i = 0; i < result.Results.Count; i += 2)
            {
                var key = result.Results[i];
                var value = result.Results[i + 1];
                to[key.FromUtf8Bytes()] = value.FromUtf8Bytes();
            }
            return to;
        }
    }
}
