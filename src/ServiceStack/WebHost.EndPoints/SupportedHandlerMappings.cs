namespace ServiceStack.WebHost.Endpoints
{
	public static class SupportedHandlerMappings
	{
		/// <summary>
		/// If you don't want your webservices to have the /servicestack/ prefix
		/// works in all known ASP.NET web hosts
		/// </summary>
		public const string CatchAllWildcard = "*";

		/// <summary>
		/// If you want the /servicestack prefix - works in IIS 7.0
		/// </summary>
		public const string ServiceStack = "servicestack";
		
		/// <summary>
		/// If you want the /servicestack prefix - Mono + FastCGI
		/// </summary>
		public const string ServiceStackWildcard = "servicestack*";
		
		/// <summary>
		/// For IIS 6.0 and before web servers. To get around the fact you can only map an .extension to ASP.NET
		/// </summary>
		public const string ServiceStackAshxForIis6 = "servicestack.ashx";
		
		/// <summary>
		/// For the VS.NET internal web development server.
		/// </summary>
		public const string ServiceStackForWebDevServer = "ServiceStack*,ServiceStack*/*,ServiceStack*/*/*,ServiceStack*/*/*/*";
	}
}