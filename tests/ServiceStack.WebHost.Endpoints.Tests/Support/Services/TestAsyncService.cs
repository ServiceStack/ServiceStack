using System;
using System.Runtime.Serialization;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
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

	public class TestAsyncService 
		: IService<TestAsync>, IAsyncService<TestAsync>
	{
		private readonly IFoo foo;

		public static int ExecuteTimes { get; private set; }
		public static int ExecuteAsyncTimes { get; private set; }
		
		public static void ResetStats()
		{
			ExecuteTimes = 0;
			ExecuteAsyncTimes = 0;
		}

		public TestAsyncService(IFoo foo)
		{
			this.foo = foo;
		}

		public object Execute(TestAsync request)
		{
			return new TestAsyncResponse { Foo = this.foo, ExecuteTimes = ++ExecuteTimes };
		}

		public object ExecuteAsync(TestAsync request)
		{
			return new TestAsyncResponse { Foo = this.foo, ExecuteAsyncTimes = ++ExecuteAsyncTimes };
		}
	}
}