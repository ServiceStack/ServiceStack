using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [Ignore("Requires ~/azureconfig.txt")]
    [TestFixture, Category("Integration")]
    public class SslTests
    {
        private string Host;
        private int Port;
        private string Password;
        private string connectionString;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var settings = new TextFileSettings("~/azureconfig.txt".MapProjectPath());
            Host = settings.GetString("Host");
            Port = settings.Get("Port", 6379);
            Password = settings.GetString("Password");
            connectionString = "{0}@{1}".Fmt(Password, Host);
        }

        [Test]
        public void Can_connect_to_azure_redis()
        {
            using (var client = new RedisClient(connectionString))
            {
                client.Set("foo", "bar");
                var foo = client.GetValue("foo");
                foo.Print();
            }
        }

        [Test]
        public void Can_connect_to_ssl_azure_redis()
        {
            using (var client = new RedisClient(connectionString))
            {
                client.Set("foo", "bar");
                var foo = client.GetValue("foo");
                foo.Print();
            }
        }

        [Test]
        public void Can_connect_to_ssl_azure_redis_with_UrlFormat()
        {
            var url = "redis://{0}?ssl=true&password={1}".Fmt(Host, Password.UrlEncode());
            using (var client = new RedisClient(url))
            {
                client.Set("foo", "bar");
                var foo = client.GetValue("foo");
                foo.Print();
            }
        }

        [Test]
        public void Can_connect_to_ssl_azure_redis_with_UrlFormat_Custom_SSL_Protocol ()
        {
            var url = "redis://{0}?ssl=true&sslprotocols=Tls12&password={1}".Fmt(Host, Password.UrlEncode());
            using (var client = new RedisClient(url))
            {
                client.Set("foo", "bar");
                var foo = client.GetValue("foo");
                foo.Print();
            }
        }

        [Test]
        public void Can_connect_to_ssl_azure_redis_with_PooledClientsManager()
        {
            using (var redisManager = new PooledRedisClientManager(connectionString))
            using (var client1 = redisManager.GetClient())
            using (var client2 = redisManager.GetClient())
            {
                client1.Set("foo", "bar");
                var foo = client2.GetValue("foo");
                foo.Print();
            }
        }

        [Test]
        public void Can_connect_to_NetworkStream()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                SendTimeout = -1,
                ReceiveTimeout = -1,
            };

            socket.Connect(Host, 6379);

            if (!socket.Connected)
            {
                socket.Close();
                throw new Exception("Could not connect");
            }

            Stream networkStream = new NetworkStream(socket);

            SendAuth(networkStream);
        }

        [Test]
        public void Can_connect_to_Buffered_SslStream()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                SendTimeout = -1,
                ReceiveTimeout = -1,
            };

            socket.Connect(Host, Port);

            if (!socket.Connected)
            {
                socket.Close();
                throw new Exception("Could not connect");
            }

            Stream networkStream = new NetworkStream(socket);

            SslStream sslStream;

            if (Env.IsMono)
            {
                //Mono doesn't support EncryptionPolicy
                sslStream = new SslStream(networkStream,
                    leaveInnerStreamOpen: false,
                    userCertificateValidationCallback: RedisConfig.CertificateValidationCallback,
                    userCertificateSelectionCallback: RedisConfig.CertificateSelectionCallback);
            }
            else
            {
                var ctor = typeof(SslStream).GetConstructors()
                    .First(x => x.GetParameters().Length == 5);

                var policyType = AssemblyUtils.FindType("System.Net.Security.EncryptionPolicy");
                var policyValue = Enum.Parse(policyType, "RequireEncryption");

                sslStream = (SslStream)ctor.Invoke(new[] {
                        networkStream,
                        false,
                        RedisConfig.CertificateValidationCallback,
                        RedisConfig.CertificateSelectionCallback,
                        policyValue,
                    });

                //sslStream = new SslStream(networkStream,
                //    leaveInnerStreamOpen: false,
                //    userCertificateValidationCallback: null,
                //    userCertificateSelectionCallback: null,
                //    encryptionPolicy: EncryptionPolicy.RequireEncryption);
            }

#if NETCORE
            sslStream.AuthenticateAsClientAsync(Host).Wait();
#else
            sslStream.AuthenticateAsClient(Host);
#endif

            if (!sslStream.IsEncrypted)
                throw new Exception("Could not establish an encrypted connection to " + Host);

            var bstream = new System.IO.BufferedStream(sslStream, 16 * 1024);

            SendAuth(bstream);
        }

        private readonly byte[] endData = new[] { (byte)'\r', (byte)'\n' };
        private void SendAuth(Stream stream)
        {
            WriteAllToStream(stream, "AUTH".ToUtf8Bytes(), Password.ToUtf8Bytes());
            ExpectSuccess(stream);
        }

        public void WriteAllToStream(Stream stream, params byte[][] cmdWithBinaryArgs)
        {
            WriteToStream(stream, GetCmdBytes('*', cmdWithBinaryArgs.Length));

            foreach (var safeBinaryValue in cmdWithBinaryArgs)
            {
                WriteToStream(stream, GetCmdBytes('$', safeBinaryValue.Length));
                WriteToStream(stream, safeBinaryValue);
                WriteToStream(stream, endData);
            }

            stream.Flush();
        }

        public void WriteToStream(Stream stream, byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }

        private static byte[] GetCmdBytes(char cmdPrefix, int noOfLines)
        {
            var strLines = noOfLines.ToString();
            var strLinesLength = strLines.Length;

            var cmdBytes = new byte[1 + strLinesLength + 2];
            cmdBytes[0] = (byte)cmdPrefix;

            for (var i = 0; i < strLinesLength; i++)
                cmdBytes[i + 1] = (byte)strLines[i];

            cmdBytes[1 + strLinesLength] = 0x0D; // \r
            cmdBytes[2 + strLinesLength] = 0x0A; // \n

            return cmdBytes;
        }

        protected void ExpectSuccess(Stream stream)
        {
            int c = stream.ReadByte();
            if (c == -1)
                throw new RedisRetryableException("No more data");

            var s = ReadLine(stream);
            s.Print();

            if (c == '-')
                throw new Exception(s.StartsWith("ERR") && s.Length >= 4 ? s.Substring(4) : s);
        }

        protected string ReadLine(Stream stream)
        {
            var sb = new StringBuilder();

            int c;
            while ((c = stream.ReadByte()) != -1)
            {
                if (c == '\r')
                    continue;
                if (c == '\n')
                    break;
                sb.Append((char)c);
            }
            return sb.ToString();
        }

        //[Conditional("DEBUG")]
        protected static void Log(string fmt, params object[] args)
        {
            //Debug.WriteLine(String.Format(fmt, args));
            Console.WriteLine(fmt, args);
        }

        [Test]
        public void SSL_can_support_64_threads_using_the_client_sequentially()
        {
            var results = 100.Times(x => ModelWithFieldsOfDifferentTypes.Create(x));
            var testData = TypeSerializer.SerializeToString(results);

            var before = Stopwatch.GetTimestamp();

            const int noOfConcurrentClients = 64; //WaitHandle.WaitAll limit is <= 64

            using (var redisClient = new RedisClient(connectionString))
            {
                for (var i = 0; i < noOfConcurrentClients; i++)
                {
                    var clientNo = i;
                    UseClient(redisClient, clientNo, testData);
                }
            }

            Debug.WriteLine(String.Format("Time Taken: {0}", (Stopwatch.GetTimestamp() - before) / 1000));
        }

        [Test]
        public void SSL_can_support_64_threads_using_the_client_simultaneously()
        {
            var results = 100.Times(x => ModelWithFieldsOfDifferentTypes.Create(x));
            var testData = TypeSerializer.SerializeToString(results);

            var before = Stopwatch.GetTimestamp();

            const int noOfConcurrentClients = 64; //WaitHandle.WaitAll limit is <= 64

            var clientAsyncResults = new List<IAsyncResult>();
            using (var manager = new PooledRedisClientManager(TestConfig.MasterHosts, TestConfig.ReplicaHosts))
            {
                manager.GetClient().Run(x => x.FlushAll());

                for (var i = 0; i < noOfConcurrentClients; i++)
                {
                    var clientNo = i;
                    var action = (Action)(() => UseClientAsync(manager, clientNo, testData));
                    clientAsyncResults.Add(action.BeginInvoke(null, null));
                }
            }

            WaitHandle.WaitAll(clientAsyncResults.ConvertAll(x => x.AsyncWaitHandle).ToArray());

            Debug.WriteLine(String.Format("Completed in {0} ticks", (Stopwatch.GetTimestamp() - before)));
        }

        private static void UseClientAsync(IRedisClientsManager manager, int clientNo, string testData)
        {
            using (var client = manager.GetReadOnlyClient())
            {
                UseClient(client, clientNo, testData);
            }
        }

        private static void UseClient(IRedisClient client, int clientNo, string testData)
        {
            var host = "";

            try
            {
                host = client.Host;

                Log("Client '{0}' is using '{1}'", clientNo, client.Host);

                var testClientKey = "test:" + host + ":" + clientNo;
                client.SetValue(testClientKey, testData);
                var result = client.GetValue(testClientKey) ?? "";

                Log("\t{0} => {1} len {2} {3} len", testClientKey,
                    testData.Length, testData.Length == result.Length ? "==" : "!=", result.Length);
            }
            catch (NullReferenceException ex)
            {
                Debug.WriteLine("NullReferenceException StackTrace: \n" + ex.StackTrace);
                Assert.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(String.Format("\t[ERROR@{0}]: {1} => {2}",
                    host, ex.GetType().Name, ex.Message));
                Assert.Fail(ex.Message);
            }
        }
    }
}