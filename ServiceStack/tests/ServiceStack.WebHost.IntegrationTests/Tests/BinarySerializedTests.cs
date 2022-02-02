using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using ServiceStack.ProtoBuf;
using ServiceStack.Text;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class BinarySerializedTests
    {
        private string RandomString(int Length)
        {
            var rnd = new Random();
            var tmp = new StringBuilder();
            for (Int64 i = 0; i < Length; i++)
            {
                tmp.Append(Convert.ToChar(((byte)rnd.Next(254))).ToString());
            }
            return Convert.ToBase64String(tmp.ToString().ToUtf8Bytes());
        }

        [Test]
        public void Can_serialize_RandomString()
        {
            var rand = RandomString(32);
            using (var ms = new MemoryStream())
            {
                global::ProtoBuf.Serializer.Serialize(ms, rand);
                ms.Position = 0;
                var fromBytes = global::ProtoBuf.Serializer.Deserialize<string>(ms);

                Assert.That(rand, Is.EqualTo(fromBytes));
            }
        }

        [Test]
        public void Can_call_cached_WebService_with_Protobuf()
        {
            var client = new ProtoBufServiceClient(Config.ServiceStackBaseUri);

            try
            {
                var fromEmail = RandomString(32);
                var response = client.Post<ProtoBufEmail>("/cached/protobuf", new CachedProtoBufEmail
                {
                    FromAddress = fromEmail
                });

                Assert.That(response.FromAddress, Is.EqualTo(fromEmail));
            }
            catch (WebServiceException webEx)
            {
                Assert.Fail(webEx.Message);
            }
        }

        [Test]
        public void Can_call_WebService_with_Protobuf()
        {
            //new ProtoBufServiceTests().Can_Send_ProtoBuf_request();

            var client = new ProtoBufServiceClient(Config.ServiceStackBaseUri);

            try
            {
                var fromEmail = RandomString(32);
                var response = client.Post<ProtoBufEmail>("/cached/protobuf", new UncachedProtoBufEmail
                {
                    FromAddress = fromEmail
                });

                Assert.That(response.FromAddress, Is.EqualTo(fromEmail));
            }
            catch (WebServiceException webEx)
            {
                Assert.Fail(webEx.Message);
            }
        }

    }
}