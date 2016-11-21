using System.Runtime.Serialization;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
    [DataContract]
    public class Test { }

    [DataContract]
    public class TestResponse
    {
        [DataMember]
        public IFoo Foo { get; set; }

        [DataMember]
        public int ExecuteTimes { get; set; }
    }

    public class TestService : IService
    {
        private readonly IFoo foo;

        public static int ExecuteTimes { get; private set; }

        public static void ResetStats()
        {
            ExecuteTimes = 0;
        }

        public TestService(IFoo foo)
        {
            this.foo = foo;
        }

        public object Any(Test request)
        {
            return new TestResponse { Foo = this.foo, ExecuteTimes = ++ExecuteTimes };
        }
    }
}