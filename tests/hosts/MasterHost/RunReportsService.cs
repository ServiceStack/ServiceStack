using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
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
			if (runType != "runall" && runType != "pathsonly" && runType != "portsonly")
				throw new ArgumentException("RunType=[runall|runpaths|runports]", "RunType");

			var runPaths = runType == "all" || runType == "pathsonly";
			if (runPaths)
			{
				foreach (var path in Config.RunOnPaths)
				{
					if (!request.Filter.IsNullOrEmpty() && path != request.Filter) continue;

					DoRequestInfo(new Report
					{
						Id = Config.PathsHostEnvironment + "-" + path,
						HostEnvironment = Config.PathsHostEnvironment,
						BaseUrl = PathUtils.CombinePaths(Config.RunOnBaseUrl, path),
					});
				}
			}

			var runPorts = runType == "all" || runType == "portsonly";
			if (runPorts)
			{
				foreach (var port in Config.RunOnPorts)
				{
					if (!request.Filter.IsNullOrEmpty() && port != request.Filter) continue;

					DoRequestInfo(new Report
					{
						Id = Config.PortsHostEnvironment + "-" + port,
						HostEnvironment = Config.PortsHostEnvironment,
						BaseUrl = Config.RunOnBaseUrl + ":" + port,
					});
				}
			}

			return base.ResolveService<ReportsService>().Get(
				new Reports { FilterHost = request.Filter });
		}

		private void DoRequestInfo(Report report)
		{
			report.LastModified = DateTime.UtcNow;
			var restClient = new JsonServiceClient(report.BaseUrl);

			foreach (var pathComponent in Config.TestPathComponents)
			{
				try
				{
					report.Id += "/" + pathComponent;
					report.RequestPath = pathComponent;

					var requestInfo = restClient.Get<RequestInfoResponse>(pathComponent);
					report.StatusCode = 200;
					report.AbsoluteUri = requestInfo.AbsoluteUri;
					report.PathInfo = requestInfo.PathInfo;
					report.RawUrl = requestInfo.RawUrl;
					report.ServiceName = requestInfo.ServiceName;
					report.UserHostAddress = requestInfo.UserHostAddress;
					report.Version = requestInfo.Version;
					report.ResponseContentType = requestInfo.ResponseContentType;
				}
				catch (Exception ex)
				{
					var webEx = ex as WebServiceException;
					if (webEx != null)
					{
						report.StatusCode = webEx.StatusCode;
						report.ErrorCode = webEx.ErrorCode;
						report.ErrorMessage = webEx.ErrorMessage;
						//report.StackTrace = webEx.ServerStackTrace;
					}
					else
					{
						report.ErrorCode = ex.GetType().Name;
						report.ErrorMessage = ex.Message;
						//report.StackTrace = ex.StackTrace;
					}
				}
				finally
				{
					DbFactory.Exec(dbCmd => dbCmd.Save(report));
				}
			}
		}
	}
}