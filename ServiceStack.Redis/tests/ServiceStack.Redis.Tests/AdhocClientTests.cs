using System;
using System.Diagnostics;
using System.Security.Cryptography;
using NUnit.Framework;
using System.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Category("Integration")]
    public class AdhocClientTests
    {
        [Test]
        public void Search_Test()
        {
            using (var client = new RedisClient(TestConfig.SingleHost))
            {
                const string cacheKey = "urn+metadata:All:SearchProProfiles?SwanShinichi Osawa /0/8,0,0,0";
                const long value = 1L;
                client.Set(cacheKey, value);
                var result = client.Get<long>(cacheKey);

                Assert.That(result, Is.EqualTo(value));
            }
        }

        public string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            var md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            var sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        [Test]
        public void Can_infer_utf8_bytes()
        {
            var cmd = "GET" + 2 + "\r\n";
            var cmdBytes = System.Text.Encoding.UTF8.GetBytes(cmd);

            var hex = BitConverter.ToString(cmdBytes);

            Debug.WriteLine(hex);

            Debug.WriteLine(BitConverter.ToString("G".ToUtf8Bytes()));
            Debug.WriteLine(BitConverter.ToString("E".ToUtf8Bytes()));
            Debug.WriteLine(BitConverter.ToString("T".ToUtf8Bytes()));
            Debug.WriteLine(BitConverter.ToString("2".ToUtf8Bytes()));
            Debug.WriteLine(BitConverter.ToString("\r".ToUtf8Bytes()));
            Debug.WriteLine(BitConverter.ToString("\n".ToUtf8Bytes()));

            var bytes = new[] { (byte)'\r', (byte)'\n', (byte)'0', (byte)'9', };
            Debug.WriteLine(BitConverter.ToString(bytes));
        }

        [Test]
        public void Convert_int()
        {
            Debug.WriteLine(BitConverter.ToString(1234.ToString().ToUtf8Bytes()));
        }

        private static byte[] GetCmdBytes1(char cmdPrefix, int noOfLines)
        {
            var cmd = cmdPrefix.ToString() + noOfLines.ToString() + "\r\n";
            return cmd.ToUtf8Bytes();
        }

        private static byte[] GetCmdBytes2(char cmdPrefix, int noOfLines)
        {
            var strLines = noOfLines.ToString();
            var cmdBytes = new byte[1 + strLines.Length + 2];
            cmdBytes[0] = (byte)cmdPrefix;

            for (var i = 0; i < strLines.Length; i++)
                cmdBytes[i + 1] = (byte)strLines[i];

            cmdBytes[cmdBytes.Length - 2] = 0x0D; // \r
            cmdBytes[cmdBytes.Length - 1] = 0x0A; // \n

            return cmdBytes;
        }

        [Test]
        public void Compare_GetCmdBytes()
        {
            var res1 = GetCmdBytes1('$', 1234);
            var res2 = GetCmdBytes2('$', 1234);

            Debug.WriteLine(BitConverter.ToString(res1));
            Debug.WriteLine(BitConverter.ToString(res2));

            var ticks1 = PerfUtils.Measure(() => GetCmdBytes1('$', 2));
            var ticks2 = PerfUtils.Measure(() => GetCmdBytes2('$', 2));

            Debug.WriteLine(String.Format("{0} : {1} = {2}", ticks1, ticks2, ticks1 / (double)ticks2));
        }

    }
}