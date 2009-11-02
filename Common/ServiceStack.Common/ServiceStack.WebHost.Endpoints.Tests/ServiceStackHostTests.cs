using System;
using System.Reflection;
using System.Runtime.Serialization;
using Funq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class ServiceStackHostTests
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

		public interface IFoo { }
		public class Foo : IFoo { }

		public class TestAppHost
			: EndpointHostBase
		{
			public TestAppHost(string serviceName, params Assembly[] assemblies)
				: base(serviceName, assemblies)
			{
			}

			public override void Configure(Container container)
			{
				container.Register<IFoo>(c => new Foo());
			}
		}

		[Test]
		public void Can_run_test_service()
		{
			var host = new TestAppHost("Example Service", typeof(TestService).Assembly);
			host.Init();

			var request = new Test();
			var response = host.ExecuteService(request) as TestResponse;

			Assert.That(response, Is.Not.Null);
			Assert.That(response.Foo, Is.Not.Null);

			TestAppHost.Instance.Dispose();
		}
	}
}