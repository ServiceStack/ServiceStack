using System;
using System.Diagnostics;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [Ignore("Diagnostic only Integration Test")]
    [TestFixture]
    public class DiagnosticTests
    {
        const string RedisServer = "or-redis02";

        const int MessageSizeBytes = 1024 * 1024;
        private const int Count = 10;

        private byte[] RandomBytes(int Length)
        {
            var rnd = new Random();
            var bytes = new byte[Length];
            for (Int64 i = 0; i < Length; i++)
            {
                bytes[i] = (byte)rnd.Next(254);
            }
            return bytes;
        }

        [Test]
        public void Test_Throughput()
        {
            var bytes = RandomBytes(MessageSizeBytes);
            var swTotal = Stopwatch.StartNew();

            var key = "test:bandwidth:" + bytes.Length;

            int bytesSent = 0;
            int bytesRecv = 0;

            using (var redisClient = new RedisNativeClient(RedisServer))
            {
                Count.Times(x =>
                {
                    var sw = Stopwatch.StartNew();

                    redisClient.Set(key, bytes);
                    bytesSent += bytes.Length;
                    "SEND {0} bytes in {1}ms".Print(bytes.Length, sw.ElapsedMilliseconds);

                    sw.Reset();
                    sw.Start();
                    var receivedBytes = redisClient.Get(key);
                    bytesRecv += receivedBytes.Length;
                    "RECV {0} bytes in {1}ms".Print(receivedBytes.Length, sw.ElapsedMilliseconds);

                    "TOTAL {0} bytes SENT {0} RECV {1} in {2}ms\n".Print(
                        bytesSent, bytesRecv, swTotal.ElapsedMilliseconds);
                });
            }
        }

    }


}