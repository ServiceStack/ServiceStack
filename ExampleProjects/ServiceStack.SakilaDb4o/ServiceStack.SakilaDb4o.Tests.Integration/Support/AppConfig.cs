using ServiceStack.Configuration;

namespace ServiceStack.SakilaDb4o.Tests.Integration.Support
{
	public class AppConfig
	{
		public string ConnectionString
		{
			get { return ConfigUtils.GetAppSetting("ConnectionString"); }
		}
	}
}