using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Logging;
using ServiceStack.MsgPack;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests;

[Route("/msgpackemail")]
[DataContract]
public class MsgPackEmail
{
    [DataMember(Order = 1)]
    public string ToAddress { get; set; }

    [DataMember(Order = 2)]
    public string FromAddress { get; set; }

    [DataMember(Order = 3)]
    public string Subject { get; set; }

    [DataMember(Order = 4)]
    public string Body { get; set; }

    [DataMember(Order = 5)]
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

[DataContract]
public class MsgPackEmailResponse
{
    [DataMember(Order = 1)]
    public ResponseStatus ResponseStatus { get; set; }
}

public class EmptyMsgPackDto
{
}

public class MsgPackEmailService : Service
{
    public object Any(MsgPackEmail request)
    {
        return request;
    }

    public object Any(EmptyMsgPackDto request)
    {
        return request;
    }
}

[TestFixture]
public class MsgPackServiceTests
{
    protected const string ListeningOn = "http://localhost:1338/";

    ExampleAppHostHttpListener appHost;

    [OneTimeSetUp]
    public void OnTestFixtureSetUp()
    {
        LogManager.LogFactory = new ConsoleLogFactory();

        appHost = new ExampleAppHostHttpListener();
        appHost.Plugins.Add(new MsgPackFormat());
        appHost.Init();
        appHost.Start(ListeningOn);
    }

    [OneTimeTearDown]
    public void OnTestFixtureTearDown()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (appHost == null) return;
        appHost.Dispose();
    }

    private static MsgPackEmail CreateMsgPackEmail()
    {
        return new MsgPackEmail
        {
            ToAddress = "to@email.com",
            FromAddress = "from@email.com",
            Subject = "Subject",
            Body = "Body",
            AttachmentData = Encoding.UTF8.GetBytes("AttachmentData"),
        };
    }

    [Test]
    public void Can_Serialize_MsgPackEmail_with_RecyclableMemoryStream()
    {
        var request = CreateMsgPackEmail();

        using var ms = MemoryStreamFactory.GetStream();
        MsgPackFormat.Serialize(request, ms);

        ms.Position = 0;
        var response = (MsgPackEmail)MsgPackFormat.Deserialize(request.GetType(), ms);

        Assert.That(response.Equals(request));
    }

    [Test]
    public void Can_Send_MsgPack_request()
    {
        var client = new MsgPackServiceClient(ListeningOn)
        {
            RequestFilter = req =>
                Assert.That(req.Accept, Is.EqualTo(MimeTypes.MsgPack))
        };

        var request = CreateMsgPackEmail();
        var response = client.Send<MsgPackEmail>(request);

        Assert.That(response.Equals(request));
    }

    [Test]
    public async Task Can_Send_MsgPack_request_Async()
    {
        var client = new MsgPackServiceClient(ListeningOn)
        {
            RequestFilter = req =>
                Assert.That(req.Accept, Is.EqualTo(MimeTypes.MsgPack))
        };

        var request = CreateMsgPackEmail();
        var response = await client.SendAsync<MsgPackEmail>(request);

        Assert.That(response.Equals(request));
    }

    [Test]
    public void Does_return_MsgPack_when_using_MsgPack_Content_Type_and_Wildcard()
    {
        var bytes = ListeningOn.CombineWith("msgpackemail")
            .PostBytesToUrl(accept: "{0}, */*".Fmt(MimeTypes.MsgPack),
                contentType: MimeTypes.MsgPack,
                requestBody: CreateMsgPackEmail().ToMsgPack(),
                responseFilter: res => Assert.That(res.MatchesContentType(MimeTypes.MsgPack)));

        Assert.That(bytes.Length, Is.GreaterThan(0));

        bytes = ListeningOn.CombineWith("msgpackemail")
            .GetBytesFromUrl(accept: "{0}, */*".Fmt(MimeTypes.MsgPack),
                responseFilter: res => Assert.That(res.MatchesContentType(MimeTypes.MsgPack)));
    }

    [Test]
    public void ToMsgPack_on_null_returns_empty_array()
    {
        MsgPackEmail email = null;
        var bytes = email.ToMsgPack();
        Assert.That(bytes, Is.Not.Null);
        Assert.That(bytes.Length, Is.EqualTo(0));
    }

    [Test]
    public void FromMsgPack_on_null_or_empty_returns_default()
    {
        byte[] nullBytes = null;
        Assert.That(nullBytes.FromMsgPack<MsgPackEmail>(), Is.Null);

        byte[] emptyBytes = [];
        Assert.That(emptyBytes.FromMsgPack<MsgPackEmail>(), Is.Null);
    }

    [Test]
    public void MsgPackFormat_Serialize_null_dto_safely_ignored()
    {
        using var ms = new MemoryStream();
        MsgPackFormat.Serialize((MsgPackEmail)null, ms);
        Assert.That(ms.Length, Is.EqualTo(0));
    }

    [Test]
    public void MsgPackFormat_Serialize_null_stream_safely_ignored()
    {
        var email = CreateMsgPackEmail();
        Assert.DoesNotThrow(() => MsgPackFormat.Serialize(email, null));
    }

    [Test]
    public void MsgPackFormat_Deserialize_guards()
    {
        Assert.Throws<ArgumentNullException>(() => MsgPackFormat.Deserialize(null, new MemoryStream()));
        Assert.That(MsgPackFormat.Deserialize(typeof(MsgPackEmail), null), Is.Null);
    }

    [Test]
    public void MsgPackServiceClient_validates_null_stream()
    {
        var client = new MsgPackServiceClient("http://localhost");
        Assert.Throws<ArgumentNullException>(() => client.SerializeToStream(null, new MsgPackEmail(), null));
        Assert.Throws<ArgumentNullException>(() => client.DeserializeFromStream<MsgPackEmail>(null));
    }

    [Test]
    public void Can_roundtrip_collections()
    {
        var list = new List<string> { "one", "two", "three" };
        var bytes = list.ToMsgPack();
        var fromList = bytes.FromMsgPack<List<string>>();
        Assert.That(fromList, Is.EqualTo(list));

        var set = new HashSet<string> { "apple", "banana", "cherry" };
        var setBytes = set.ToMsgPack();
        var fromSet = setBytes.FromMsgPack<HashSet<string>>();
        Assert.That(fromSet, Is.EquivalentTo(set));
    }

    [Test]
    public void Empty_DTO_normalizes_to_empty_instance()
    {
        var empty = new EmptyMsgPackDto();
        var bytes = empty.ToMsgPack();

        var fromBytes = bytes.FromMsgPack<EmptyMsgPackDto>();
        Assert.That(fromBytes, Is.Not.Null);
        Assert.That(fromBytes, Is.InstanceOf<EmptyMsgPackDto>());
    }

    [Test]
    public void Concurrent_MsgPack_serialization_is_safe()
    {
        var tasks = new Task[10];
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                var email = CreateMsgPackEmail();
                var bytes = email.ToMsgPack();
                var fromBytes = bytes.FromMsgPack<MsgPackEmail>();
                Assert.That(fromBytes.Equals(email));

                var empty = new EmptyMsgPackDto();
                var emptyBytes = empty.ToMsgPack();
                var fromEmpty = emptyBytes.FromMsgPack<EmptyMsgPackDto>();
                Assert.That(fromEmpty, Is.Not.Null);

                var list = new List<string> { "a", "b" };
                var listBytes = list.ToMsgPack();
                var fromList = listBytes.FromMsgPack<List<string>>();
                Assert.That(fromList, Is.EqualTo(list));
            });
        }
        Task.WaitAll(tasks);
    }
}
