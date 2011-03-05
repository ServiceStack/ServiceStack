using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using ServiceStack;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Support;

namespace MasterHost
{
	[DataContract]
	[Description("RunType=[all|pathsonly|portsonly]")]
	[RestService("/requestinfo")]
	[RestService("/requestinfo/{RunType}")]
	public class RunRequestInfo
	{
		[DataMember]
		public string RunType { get; set; }

		[DataMember]
		public string Filter { get; set; }
	}

	[DataContract]
	public class RunRequestInfoResponse : IHasResponseStatus
	{
		public RunRequestInfoResponse()
		{
			this.ResponseStatus = new ResponseStatus();

			this.Results = new List<RequestInfoResponse>();
		}

		[DataMember]
		public List<RequestInfoResponse> Results { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}

	public class RunRequestInfoService : RestServiceBase<RunRequestInfo>
	{
		static RunRequestInfoService()
		{
			//Tell OrmLite what to use for the primary key
			ModelConfig<RequestInfoResponse>.Id(x => x.Host);
		}

		public AppConfig Config { get; set; }

		public IDbConnectionFactory DbFactory { get; set; }

		public override object OnGet(RunRequestInfo request)
		{
			if (!request.RunType.IsNullOrEmpty())
			{
				var runType = request.RunType.ToLower();
				if (runType != "all" && runType != "pathsonly" && runType != "portsonly")
					throw new ArgumentException("Required [ all | pathsonly | portsonly ]", "RunType");

				var runPaths = runType == "all" || runType == "pathsonly";
				var runPorts = runType == "all" || runType == "portsonly";

				for (var i = 0; i < Config.HandlerHosts.Count; i++)
				{
					var hostPath = Config.HandlerHosts[i];
					var hostName = Config.HandlerHostNames[i];

					if (!request.Filter.IsNullOrEmpty() && hostPath != request.Filter) continue;

					var isPort = hostPath.Contains(":");
					var isPath = !isPort;
					if (isPort && !runPorts) continue;
					if (isPath && !runPaths) continue;

					var baseUrl = Config.RunOnBaseUrl + hostPath;
					DoRequestInfo(hostName, hostPath, baseUrl, new[] { "/_requestinfo" });
				}
			}

			return new RunRequestInfoResponse
			{
				Results = DbFactory.Exec(dbCmd => dbCmd.Select<RequestInfoResponse>())
			};
		}

		public void DoRequestInfo(string hostName, string hostPath, string baseUrl, IEnumerable<string> testPaths)
		{
			foreach (var testPath in testPaths)
			{
				var requestUrl = PathUtils.CombinePaths(baseUrl, testPath);
				RequestInfoResponse requestInfo = null;

				try
				{
					var webReq = (HttpWebRequest)WebRequest.Create(requestUrl);
					webReq.Accept = ContentType.Json;
					var webRes = (HttpWebResponse)webReq.GetResponse();

					var contents = new StreamReader(webRes.GetResponseStream()).ReadToEnd();

					if (webRes.ContentType.StartsWith(ContentType.Json))
					{
						requestInfo = JsonSerializer.DeserializeFromString<RequestInfoResponse>(contents);
					}
					else
					{
						requestInfo = new RequestInfoResponse
						{												
							Host = requestUrl,
							Path = testPath,
							ContentType = webRes.ContentType,
						};
					}
					requestInfo.Host = "<h4><b>" + testPath + "</b></h4>" + requestInfo.Host;
					requestInfo.Status = (int)webRes.StatusCode;
					requestInfo.ContentLength = webRes.ContentLength;
				}
				catch (Exception ex)
				{
					requestInfo = new RequestInfoResponse
					{
						Host = "<h4><b>" + testPath + "</b></h4>" + requestUrl,
						Path = testPath,
						ErrorCode = ex.GetType().Name,
						ErrorMessage = ex.Message,
						Status = 600,
					};
					var webEx = ex as WebException;
					if (webEx != null)
					{
						requestInfo.Status = (int)webEx.Status;
					}
				}
				finally
				{
					DbFactory.Exec(dbCmd => dbCmd.Save(requestInfo));
				}
			}
		}
	}

}