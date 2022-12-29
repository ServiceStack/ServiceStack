using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackExchangeTester
{
    class Program
    {
        static void Main(string[] args)
        {
            var x = new RedisManagerPool("MQHnkdl402DScXzhZIxHwDaA7s8nziy45okp84ykShA=@tls-11.redis.cache.windows.net:6380?ssl=true&sslprotocols=Tls11");
            var y = x.GetClient();
            y.Ping();
            y.Set<string>("keyServiceStackSllChangesIStillHave512mb", "value");
            y.Dispose();
            x.Dispose();
        }
    }
}
