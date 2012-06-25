using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Common;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.ServiceInterface.Admin
{
    public class RequestLogs
    {
        public int? BeforeSecs { get; set; }
        public int? AfterSecs { get; set; }
        public string IpAddress { get; set; }
        public string ForwardedFor { get; set; }
        public string UserAuthId { get; set; }
        public string SessionId { get; set; }
        public string Referer { get; set; }
        public string PathInfo { get; set; }
        public long[] Ids { get; set; }
        public int? BeforeId { get; set; }
        public int? AfterId { get; set; }
        public bool? HasResponse { get; set; }
        public bool? WithErrors { get; set; }
        public int Skip { get; set; }
        public int? Take { get; set; }
        public bool? EnableSessionTracking { get; set; }
        public bool? EnableResponseTracking { get; set; }
        public bool? EnableErrorTracking { get; set; }
        public TimeSpan? DurationLongerThan { get; set; }
        public TimeSpan? DurationLessThan { get; set; }
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

    public class RequestLogsService : ServiceBase<RequestLogs>
    {
        private static readonly Dictionary<string, string> Usage = new Dictionary<string, string> {
            {"int BeforeSecs",      "Requests before elapsed time"},
            {"int AfterSecs",       "Requests after elapsed time"},
            {"string IpAddress",    "Requests matching Ip Address"},
            {"string ForwardedFor", "Requests matching Forwarded Ip Address"},
            {"string UserAuthId",   "Requests matching UserAuthId"},
            {"string SessionId",    "Requests matching SessionId"},
            {"string Referer",      "Requests matching Http Referer"},
            {"string PathInfo",     "Requests matching PathInfo"},
            {"int BeforeId",        "Requests before RequestLog Id"},
            {"int AfterId",         "Requests after RequestLog Id"},
            {"bool WithErrors",     "Requests with errors"},
            {"int Skip",            "Skip past N results"},
            {"int Take",            "Only look at last N results"},
            {"bool EnableSessionTracking",  "Turn On/Off Session Tracking"},
            {"bool EnableResponseTracking", "Turn On/Off Tracking of Responses"},
            {"bool EnableErrorTracking",    "Turn On/Off Tracking of Errors"},
            {"TimeSpan DurationLongerThan", "Requests with a duration longer than"},
            {"TimeSpan DurationLessThan", "Requests with a duration less than"},
        };

        protected override object Run(RequestLogs request)
        {
            if (RequestLogger == null)
                throw new Exception("No IRequestLogger is registered");

            RequiredRoleAttribute.AssertRequiredRoles(RequestContext, RequestLogger.RequiredRoles);

            if (request.EnableSessionTracking.HasValue)
                RequestLogger.EnableSessionTracking = request.EnableSessionTracking.Value;

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
            if (request.WithErrors.HasValue)
                logs = request.WithErrors.Value
                    ? logs.Where(x => x.ErrorResponse != null)
                    : logs.Where(x => x.ErrorResponse == null);
            if (request.DurationLongerThan.HasValue)
                logs = logs.Where(x => x.RequestDuration > request.DurationLongerThan.Value);
            if (request.DurationLessThan.HasValue)
                logs = logs.Where(x => x.RequestDuration < request.DurationLessThan.Value);

            var results = logs.Skip(request.Skip).OrderByDescending(x => x.Id).ToList();

            return new RequestLogsResponse {
                Results = results,
                Usage = Usage,
            };
        }
    }
}