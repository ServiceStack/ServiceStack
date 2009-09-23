using RemoteInfo.ServiceModel.Operations;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;

namespace RemoteInfo.Tests.ConsoleClient
{
	class Program
	{
		//IIS / mod_mono / FastCGI WebService endpoints:
		private const string JsonServiceBaseUrl = "http://localhost/RemoteInfo.Host.Web/Public/Json/SyncReply/";
		private const string XmlServiceBaseUrl = "http://localhost/RemoteInfo.Host.Web/Public/Xml/SyncReply/";

		//RemoteInfo.Host.Console ConsoleService endpoints:
		private const string ConsoleHostXmlServiceBaseUrl = "http://localhost:81/";

		public static IServiceClient GetServiceClient(bool useConsoleHost, bool useJson)
		{
			return useJson ? (IServiceClient)new JsonServiceClient(JsonServiceBaseUrl)
						   : new XmlServiceClient(useConsoleHost ? ConsoleHostXmlServiceBaseUrl : XmlServiceBaseUrl);
		}

		static void Main(string[] args)
		{
			LogManager.LogFactory = new ConsoleLogFactory();
			var log = LogManager.GetLogger(typeof(Program));

			var useConsoleHost = args.Length > 0 ? args[0].Equals("-console-host") : false;
			var useJson = !useConsoleHost && args.Length > 0 ? args[0].Equals("-json") : false;

			var viewRemotePath = args.Length > 1 ? args[1] : "/Server";

			log.InfoFormat("Usage: [-json|-console-host|-xml] /remote/path\n");

			log.InfoFormat("Viewing {0} GetDirectoryInfo on '{1}' using '{2}'\n", 
				useConsoleHost ? "ConsoleService" : "WebService", viewRemotePath, useJson ? "JSON" : "XML");

			using (var serviceClient = GetServiceClient(useConsoleHost, useJson))
			{
				var request = new GetDirectoryInfo { ForPath = viewRemotePath };
				var response = serviceClient.Send<GetDirectoryInfoResponse>(request);

				foreach (var dir in response.Directories)
				{
					log.InfoFormat("/{0} \t   ({1})", dir.Name.PadRight(45, ' '), dir.FileCount.ToString());
				}

				foreach (var file in response.Files)
				{
					log.InfoFormat(" + {0} \t{1} bytes", file.Name.PadRight(45, ' '), file.FileSizeBytes.ToString().PadLeft(6,' '));
				}
			}

		}

	}

}
