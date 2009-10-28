using System.Runtime.Serialization;

namespace ServiceStack.ServiceHost.Tests.Support
{
	[DataContract]
	public class AutoWireServiceRequest { }

	public class AutoWireService 
		: IService<AutoWireServiceRequest>
	{
		private readonly IFoo foo;

		public IFoo Foo
		{
			get { return foo; }
		}

		public IBar Bar { get; set; }

		public AutoWireService(IFoo foo)
		{
			this.foo = foo;
		}

		public object Execute(AutoWireServiceRequest request)
		{
			return new AutoWireServiceRequest();
		}
	}

	public class Foo : IFoo
	{
	}

	public interface IFoo
	{
	}

	public class Bar : IBar
	{
	}

	public interface IBar
	{
	}
}