using System.Net;
using System.Runtime.Serialization;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services;

[DataContract]
public class TestAsync { }

[DataContract]
public class TestAsyncResponse
{
    [DataMember]
    public IFoo Foo { get; set; }

    [DataMember]
    public int ExecuteTimes { get; set; }

    [DataMember]
    public int ExecuteAsyncTimes { get; set; }
}

[Route("/returnsvoid")]
[DataContract]
public class ReturnsVoid : IReturnVoid
{
    [DataMember]
    public string Message { get; set; }
}

[Route("/returnswebresponse")]
[DataContract]
public class ReturnsWebResponse : IReturn<HttpWebResponse>
{
    [DataMember]
    public string Message { get; set; }
}

public class TestAsyncService(IFoo foo) : IService
{
    public static int ExecuteTimes { get; private set; }
    public static int ExecuteAsyncTimes { get; private set; }
    public static string ReturnVoidMessage;
    public static string ReturnWebResponseMessage;

    public static void ResetStats()
    {
        ExecuteTimes = 0;
        ExecuteAsyncTimes = 0;
    }

    public object Any(TestAsync request)
    {
        return new TestAsyncResponse { Foo = foo, ExecuteTimes = ++ExecuteTimes };
    }

    public void Any(ReturnsVoid request)
    {
        ReturnVoidMessage = request.Message;
    }

    public void Any(ReturnsWebResponse request)
    {
        ReturnWebResponseMessage = request.Message;
    }
}