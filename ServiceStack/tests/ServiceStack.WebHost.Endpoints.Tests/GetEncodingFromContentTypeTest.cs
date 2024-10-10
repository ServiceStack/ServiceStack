#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.Host.HttpListener;

namespace ServiceStack.WebHost.Endpoints.Tests.TestExistingDir;

[TestFixture]
public class GetEncodingFromContentTypeTest
{

    [Test]
    public void Can_Get_Correct_Encoding()
    {
        var ct = "Content-Type: text/plain; charset=KOI8-R";

        var encoding = ListenerRequest.GetEncoding(ct);

        Assert.AreEqual("koi8-r", encoding.BodyName);

    }

    [Test]
    public void Return_Null_When_No_Encoding()
    {
        var ct = "Content-Type: text/plain";

        var encoding = ListenerRequest.GetEncoding(ct);

        Assert.IsNull(encoding);

    }

    [Test]
    public void Return_Null_When_Wrong_Encoding()
    {
        var ct = "Content-Type: text/plain; charset=ASDFG";

        var encoding = ListenerRequest.GetEncoding(ct);

        Assert.IsNull(encoding);

    }
}

#endif
