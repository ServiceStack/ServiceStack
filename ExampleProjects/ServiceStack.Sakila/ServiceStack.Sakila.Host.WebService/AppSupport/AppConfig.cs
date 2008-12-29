using System.Configuration;

namespace ServiceStack.Sakila.Host.WebService.AppSupport
{
	public class AppConfig
	{
		public string ConnectionString
		{
			get { return ConfigurationManager.AppSettings["ConnectionString"]; }
		}

		public string ServerPrivateKey
		{
			get { return ConfigurationManager.AppSettings["ServerPrivateKey"]; }
		}

		public string UsageExamplesBaseUri
		{
			get { return ConfigurationManager.AppSettings["UsageExamplesBaseUri"]; }
		}

		public static string StringResourcesFile
		{
			get { return ConfigurationManager.AppSettings["StringResourcesFile"]; }
		}
	}
}