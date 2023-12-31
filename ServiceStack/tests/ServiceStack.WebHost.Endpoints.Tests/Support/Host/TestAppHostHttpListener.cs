using Funq;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Host;

public class TestAppHostHttpListener
	: AppHostHttpListenerBase
{
	public TestAppHostHttpListener()
		: base("Example Service", typeof(TestService).Assembly)
	{
		Instance = null;
	}

	public override void Configure(Container container)
	{
		container.Register<IFoo>(c => new Foo());
	}
}
