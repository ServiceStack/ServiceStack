using System.Collections.Generic;
using System.Globalization;

namespace ServiceStack.Redis
{
    public static class RedisDataExtensions
    {
        public static RedisText ToRedisText(this RedisData data)
        {
            if (data == null) return null; //In Transaction

            var to = new RedisText();

            if (data.Data != null)
                to.Text = data.Data.FromUtf8Bytes();

            if (data.Children != null)
                to.Children = data.Children.ConvertAll(x => x.ToRedisText());

            return to;
        }

        public static double ToDouble(this RedisData data)
            => double.Parse(data.Data.FromUtf8Bytes(),
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture);

        public static long ToInt64(this RedisData data)
            => long.Parse(data.Data.FromUtf8Bytes(),
                          NumberStyles.Integer,
                          CultureInfo.InvariantCulture);

        public static string GetResult(this RedisText from) => from.Text;

        public static T GetResult<T>(this RedisText from) => from.Text.FromJson<T>();

        public static List<string> GetResults(this RedisText from)
            => from.Children == null
               ? new List<string>()
               : from.Children.ConvertAll(x => x.Text);

        public static List<T> GetResults<T>(this RedisText from)
            => from.Children == null
               ? new List<T>()
               : from.Children.ConvertAll(x => x.Text.FromJson<T>());
    }
}