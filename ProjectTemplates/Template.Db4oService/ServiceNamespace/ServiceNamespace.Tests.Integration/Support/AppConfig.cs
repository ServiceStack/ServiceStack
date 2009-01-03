using ServiceStack.Configuration;

namespace @ServiceNamespace@.Tests.Integration.Support
{
	public class AppConfig
	{
		public string ConnectionString
		{
			get { return ConfigUtils.GetAppSetting("ConnectionString"); }
		}
	}
}