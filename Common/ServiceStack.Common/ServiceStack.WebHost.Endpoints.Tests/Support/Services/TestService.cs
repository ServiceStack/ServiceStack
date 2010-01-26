using System.Runtime.Serialization;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
	[DataContract]
	public class Test { }

	[DataContract]
	public class TestResponse
	{
		public IFoo Foo { get; set; }
	}

	public class TestService : IService<Test>
	{
		private readonly IFoo foo;

		public TestService(IFoo foo)
		{
			this.foo = foo;
		}

		public object Execute(Test request)
		{
			return new TestResponse { Foo = this.foo };
		}
	}
}