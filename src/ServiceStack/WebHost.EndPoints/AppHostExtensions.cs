namespace ServiceStack.WebHost.Endpoints
{
	public static class AppHostExtensions
	{
		public static void RegisterService<TService>(this IAppHost appHost, params string[] atRestPaths)
		{
			appHost.RegisterService(typeof(TService), atRestPaths);
		}
	}
}