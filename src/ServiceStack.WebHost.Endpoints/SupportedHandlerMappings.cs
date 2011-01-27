namespace ServiceStack.WebHost.Endpoints
{
	public static class SupportedHandlerMappings
	{
		public const string CatchAllWildcard = "*";
		public const string ServiceStack = "servicestack";
		public const string ServiceStackWildcard = "servicestack*";
		public const string ServiceStackAshxForIis6 = "servicestack.ashx";
		public const string ServiceStackForWebDevServer = "ServiceStack*,ServiceStack*/*,ServiceStack*/*/*,ServiceStack*/*/*/*";
	}
}