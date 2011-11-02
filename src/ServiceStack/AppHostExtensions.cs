using ServiceStack.WebHost.Endpoints;

namespace ServiceStack
{
	public static class AppHostExtensions
	{
		 public static void RegisterService<TService>(this IAppHost appHost)
		 {
		 	appHost.RegisterService(typeof(TService));
		 }
	}
}