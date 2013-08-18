using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using MsgPack;
using MsgPack.Serialization;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.Plugins.MsgPack;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;

namespace RazorRockstars.Console.Files
{
    public class MsgPackEmail
    {
        public string ToAddress { get; set; }
        public string FromAddress { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public byte[] AttachmentData { get; set; }

        public bool Equals(MsgPackEmail other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.ToAddress, ToAddress)
                && Equals(other.FromAddress, FromAddress)
                && Equals(other.Subject, Subject)
                && Equals(other.Body, Body)
                && other.AttachmentData.EquivalentTo(AttachmentData);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(MsgPackEmail)) return false;
            return Equals((MsgPackEmail)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = (ToAddress != null ? ToAddress.GetHashCode() : 0);
                result = (result * 397) ^ (FromAddress != null ? FromAddress.GetHashCode() : 0);
                result = (result * 397) ^ (Subject != null ? Subject.GetHashCode() : 0);
                result = (result * 397) ^ (Body != null ? Body.GetHashCode() : 0);
                result = (result * 397) ^ (AttachmentData != null ? AttachmentData.GetHashCode() : 0);
                return result;
            }
        }
    }

    public class MsgPackEmailResponse
    {
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class MsgPackEmailService : Service
    {
        public object Any(MsgPackEmail request)
        {
            return request;
        }
    }


    [TestFixture]
    public class MsgPackServiceTests
    {
        protected const string ListeningOn = "http://localhost:85/";

        AppHost appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            LogManager.LogFactory = new ConsoleLogFactory();

            appHost = new AppHost { EnableRazor = false };
            appHost.Plugins.Add(new MsgPackFormat());
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (appHost == null) return;
            appHost.Dispose();
            appHost = null;
        }

        MsgPackEmail request = new MsgPackEmail {
            ToAddress = "to@email.com",
            FromAddress = "from@email.com",
            Subject = "Subject",
            Body = "Body",
            AttachmentData = Encoding.UTF8.GetBytes("AttachmentData"),
        };

        [Test]
        public void Can_Send_MsgPack_request()
        {
            var client = new MsgPackServiceClient(ListeningOn);

            try
            {
                var response = client.Send<MsgPackEmail>(request);

                response.PrintDump();

                Assert.That(response.Equals(request));
            }
            catch (WebServiceException webEx)
            {
                webEx.ResponseDto.PrintDump();
                Assert.Fail(webEx.Message);
            }
        }

        [Test]
        public void Can_serialize_email_dto()
        {
            using (var ms = new MemoryStream())
            {
                var serializer = MessagePackSerializer.Create(request.GetType());
                serializer.PackTo(Packer.Create(ms), request);

                ms.Position = 0;

                var unpacker = Unpacker.Create(ms);
                unpacker.Read();
                var response = serializer.UnpackFrom(unpacker);

                Assert.That(response.Equals(request));
            }
        }

        [Test]
        public void Can_serialize_email_dto_generic()
        {
            using (var ms = new MemoryStream())
            {
                var serializer = MessagePackSerializer.Create<MsgPackEmail>();
                serializer.Pack(ms, request);

                ms.Position = 0;

                var response = serializer.Unpack(ms);

                Assert.That(response.Equals(request));
            }
        }

    }
}