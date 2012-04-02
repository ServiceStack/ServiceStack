using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.ServiceInterface.Admin
{
	public class RequestLogs
	{
		public int? BeforeSecs { get; set; }
		public int? AfterSecs { get; set; }
		public string IpAddress { get; set; }
		public string UserAuthId { get; set; }
		public string SessionId { get; set; }
		public string Referer { get; set; }
		public string PathInfo { get; set; }
		public long[] Ids { get; set; }
		public int? BeforeId { get; set; }
		public int? AfterId { get; set; }
		public int Skip { get; set; }
		public int? Take { get; set; }
	}

	public class RequestLogsResponse
	{
		public RequestLogsResponse()
		{
			this.Results = new List<RequestLogEntry>();
		}

		public List<RequestLogEntry> Results { get; set; }
		public Dictionary<string, string> Usage { get; set; }
		public ResponseStatus ResponseStatus { get; set; }
	}

	[RequiredRole(RoleNames.Admin)]
	public class RequestLogsService : ServiceBase<RequestLogs>
	{
		public IRequestLogger RequestLogger { get; set; }

		private static readonly Dictionary<string,string> Usage = new Dictionary<string,string> {
			{"BeforeSecs", "int - Requests before elapsed time"},
			{"AfterSecs",  "int - Requests after elapsed time"},
			{"IpAddress",  "string - Requests matcing Ip Address"},
			{"UserAuthId", "string - Requests matcing UserAuthId"},
			{"SessionId",  "string - Requests matcing SessionId"},
			{"Referer",    "string - Requests matcing Http Referer"},
			{"PathInfo",   "string - Requests matcing PathInfo"},
			{"BeforeId",   "int - Requests before RequestLog Id"},
			{"AfterId",    "int - Requests after RequestLog Id"},
			{"Skip",       "int - Skip past N results"},
			{"Take",       "int - Only look at last N results"},
		};

		protected override object Run(RequestLogs request)
		{
			if (RequestLogger == null)
				throw new Exception("No IRequestLogger is registered");

			var now = DateTime.UtcNow;
			var logs = RequestLogger.GetLatestLogs(request.Take).AsQueryable();

			if (request.BeforeSecs.HasValue)
				logs = logs.Where(x => (now - x.DateTime) <= TimeSpan.FromSeconds(request.BeforeSecs.Value));
			if (request.AfterSecs.HasValue)
				logs = logs.Where(x => (now - x.DateTime) > TimeSpan.FromSeconds(request.AfterSecs.Value));
			if (!request.IpAddress.IsNullOrEmpty())
				logs = logs.Where(x => x.IpAddress == request.IpAddress);
			if (!request.UserAuthId.IsNullOrEmpty())
				logs = logs.Where(x => x.UserAuthId == request.UserAuthId);
			if (!request.SessionId.IsNullOrEmpty())
				logs = logs.Where(x => x.SessionId == request.SessionId);
			if (!request.Referer.IsNullOrEmpty())
				logs = logs.Where(x => x.Referer == request.Referer);
			if (!request.PathInfo.IsNullOrEmpty())
				logs = logs.Where(x => x.PathInfo == request.PathInfo);
			if (!request.Ids.IsEmpty())
				logs = logs.Where(x => request.Ids.Contains(x.Id));
			if (request.BeforeId.HasValue)
				logs = logs.Where(x => x.Id <= request.BeforeId);
			if (request.AfterId.HasValue)
				logs = logs.Where(x => x.Id > request.AfterId);

			var results = logs.Skip(request.Skip).OrderByDescending(x => x.Id).ToList();

			return new RequestLogsResponse {
				Results = results,
				Usage = Usage,
			};
		}
	}
}