using RemoteInfo.ServiceModel.Operations;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;

namespace RemoteInfo.Tests.ConsoleClient
{
	/// <summary>
	///	This is a Command Line program that calls the webservice in C#
	///
	/// It allows you to call the web service when running:
	///   - hosted in IIS 6 or 7, apache using mod_mono or other browsers that support FastCGI
	///   - hosted in xsp
	///   - hosted inside a Console Application (i.e. without a web server)
	/// 
	/// It also allows you to run the same program using either JSON or XML.
	/// 
	/// </summary>
	class Program
	{
		const bool UsingXspWebServer = true;

		const string DefaultBaseUrl = "http://localhost/RemoteInfo.Host.Web";
		const string XspBaseUrl = "http://localhost:8080";
		const string WebServerBaseUrl = UsingXspWebServer ? XspBaseUrl : DefaultBaseUrl;
		
		//IIS / mod_mono / FastCGI WebService endpoints:
		private const string JsonServiceBaseUrl = WebServerBaseUrl + "/Public/Json/SyncReply/";
		private const string XmlServiceBaseUrl  = WebServerBaseUrl + "/Public/Xml/SyncReply/";
		
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
