using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.Serialization;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.OrmLite;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.WebHost.Endpoints.Support;

namespace MasterHost
{
	[DataContract]
	[Description("RunType=[all|pathsonly|portsonly], Filter=[PathHostNameFilter|PortFilter], e.g [handler.wildcard35|5000]")]
	[RestService("/runreports/{RunType}")]
	[RestService("/runreports/{RunType}/{Filter}")]
	public class RunReports
	{
		[DataMember]
		public string RunType { get; set; }

		[DataMember]
		public string Filter { get; set; }
	}

	[DataContract]
	public class RunReportsResponse : IHasResponseStatus
	{
		public RunReportsResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}

	public class RunReportsService : RestServiceBase<RunReports>
	{
		public AppConfig Config { get; set; }

		public IDbConnectionFactory DbFactory { get; set; }

		public override object OnGet(RunReports request)
		{
			if (request.RunType.IsNullOrEmpty())
				throw new ArgumentNullException("RunType");

			var runType = request.RunType.ToLower();
			if (runType != "all" && runType != "pathsonly" && runType != "portsonly")
				throw new ArgumentException("RunType=[all|pathsonly|portsonly]", "RunType");

			var runPaths = runType == "all" || runType == "pathsonly";
			if (runPaths)
			{
				foreach (var path in Config.AllPaths)
				{
					if (!request.Filter.IsNullOrEmpty() && path != request.Filter) continue;

					DoRequestInfo(new Report
						{
							Id = Config.PathsHostEnvironment + "-" + path,
							HostEnvironment = Config.PathsHostEnvironment,
							BaseUrl = PathUtils.CombinePaths(Config.RunOnBaseUrl, path),
						},
						Config.GetPathsForPath(path));
				}
			}

			var runPorts = runType == "all" || runType == "portsonly";
			if (runPorts)
			{
				foreach (var port in Config.AllPorts)
				{
					if (!request.Filter.IsNullOrEmpty() && port != request.Filter) continue;

					DoRequestInfo(new Report
						{
							Id = Config.PortsHostEnvironment + "-" + port,
							HostEnvironment = Config.PortsHostEnvironment,
							BaseUrl = Config.RunOnBaseUrl + ":" + port,
						},
						Config.GetPathsForPort(port));
				}
			}

			return base.ResolveService<ReportsService>().Get(
				new Reports { FilterHost = request.Filter });
		}

		private void DoRequestInfo(Report report, IEnumerable<string> testPaths)
		{
			report.LastModified = DateTime.UtcNow;
			var restClient = new JsonServiceClient(report.BaseUrl);

			report.MaxStatusCode = 0;
			foreach (var testPath in testPaths)
			{
				var test = new ReportTest { RequestPath = testPath };
				report.Tests.Add(test);
				try
				{
					if (testPath.Contains("_requestinfo"))
					{
						var requestInfo = restClient.Get<RequestInfoResponse>(testPath);

						if (report.ServiceName == null)
						{
							report.ServiceName = requestInfo.ServiceName;
							report.UserHostAddress = requestInfo.UserHostAddress;
							report.Version = requestInfo.Version;
						}

						test.StatusCode = 200;
						test.AbsoluteUri = requestInfo.AbsoluteUri;
						test.PathInfo = requestInfo.PathInfo;
						test.RawUrl = requestInfo.RawUrl;
						test.ResponseContentType = requestInfo.ResponseContentType;
					}
					else
					{
						var webReq = (HttpWebRequest)WebRequest.Create(report.BaseUrl + testPath);
						webReq.Accept = ContentType.Json;

						var webRes = (HttpWebResponse)webReq.GetResponse();
						test.ResponseContentType = webRes.ContentType;
						test.StatusCode = (int)webRes.StatusCode;
					}

					report.MaxStatusCode = Math.Max(report.MaxStatusCode, (int)HttpStatusCode.OK);
				}
				catch (WebServiceException webEx)
				{
					report.MaxStatusCode = Math.Max(report.MaxStatusCode, webEx.StatusCode);
					test.StatusCode = webEx.StatusCode;
					test.ErrorCode = webEx.ErrorCode;
					test.ErrorMessage = webEx.ErrorMessage;
					//report.StackTrace = webEx.ServerStackTrace;
				}
				catch (Exception ex)
				{
					var webEx = ex as WebException;
					if (webEx != null)
					{
						report.MaxStatusCode = Math.Max(report.MaxStatusCode, (int)webEx.Status);

						test.ErrorCode = ex.GetType().Name;
						test.ErrorMessage = ex.Message;
					}
					else
					{
						report.MaxStatusCode = Math.Max(report.MaxStatusCode, 600);

						test.ErrorCode = ex.GetType().Name;
						test.ErrorMessage = ex.Message;
					}
					//report.StackTrace = ex.StackTrace;
				}
				finally
				{
					DbFactory.Exec(dbCmd => dbCmd.Save(report));
				}
			}
		}
	}
}