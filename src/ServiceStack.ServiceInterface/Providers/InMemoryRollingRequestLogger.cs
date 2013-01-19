using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ServiceStack.Common.Net30;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.ServiceModel;

namespace ServiceStack.ServiceInterface.Providers
{
    public class InMemoryRollingRequestLogger : IRequestLogger
    {
        private static int requestId = 0;

        public const int DefaultCapacity = 1000;
        private readonly ConcurrentQueue<RequestLogEntry> logEntries = new ConcurrentQueue<RequestLogEntry>();
        readonly int capacity;

        public bool EnableSessionTracking { get; set; }

        public bool EnableRequestBodyTracking { get; set; }

        public bool EnableResponseTracking { get; set; }

        public bool EnableErrorTracking { get; set; }

        public string[] RequiredRoles { get; set; }

        public Type[] ExcludeRequestDtoTypes { get; set; }

        public Type[] HideRequestBodyForRequestDtoTypes { get; set; }

        public InMemoryRollingRequestLogger(int? capacity = DefaultCapacity)
        {
            this.capacity = capacity.GetValueOrDefault(DefaultCapacity);
        }

        public void Log(IRequestContext requestContext, object requestDto, object response, TimeSpan requestDuration)
        {
            var requestType = requestDto != null ? requestDto.GetType() : null;

            if (ExcludeRequestDtoTypes != null
                && requestType != null
                && ExcludeRequestDtoTypes.Contains(requestType))
                return;
                
            var entry = new RequestLogEntry {
                Id = Interlocked.Increment(ref requestId),
                DateTime = DateTime.UtcNow,
                RequestDuration = requestDuration,
            };

            var httpReq = requestContext != null ? requestContext.Get<IHttpRequest>() : null;
            if (httpReq != null)
            {
                entry.HttpMethod = httpReq.HttpMethod;
                entry.AbsoluteUri = httpReq.AbsoluteUri;
                entry.PathInfo = httpReq.PathInfo;
                entry.IpAddress = requestContext.IpAddress;
                entry.ForwardedFor = httpReq.Headers[HttpHeaders.XForwardedFor];
                entry.Referer = httpReq.Headers[HttpHeaders.Referer];
                entry.Headers = httpReq.Headers.ToDictionary();
                entry.UserAuthId = httpReq.GetItemOrCookie(HttpHeaders.XUserAuthId);
                entry.SessionId = httpReq.GetSessionId();
                entry.Items = httpReq.Items;
                entry.Session = EnableSessionTracking ? httpReq.GetSession() : null;
            }

            if (HideRequestBodyForRequestDtoTypes != null
                && requestType != null
                && !HideRequestBodyForRequestDtoTypes.Contains(requestType)) 
            {
                entry.RequestDto = requestDto;
                if (httpReq != null)
                {
                    entry.FormData = httpReq.FormData.ToDictionary();

                    if (EnableRequestBodyTracking)
                    {
                        entry.RequestBody = httpReq.GetRawBody();
                    }
                }
            }
            if (!response.IsErrorResponse()) {
                if (EnableResponseTracking)
                    entry.ResponseDto = response;
            }
            else {
                if (EnableErrorTracking)
                    entry.ErrorResponse = ToSerializableErrorResponse(response);
            }

            logEntries.Enqueue(entry);

            RequestLogEntry dummy;
            if (logEntries.Count > capacity)
                logEntries.TryDequeue(out dummy);
        }

        public List<RequestLogEntry> GetLatestLogs(int? take)
        {
            var requestLogEntries = logEntries.ToArray();			
            return take.HasValue 
                ? new List<RequestLogEntry>(requestLogEntries.Take(take.Value))
                : new List<RequestLogEntry>(requestLogEntries);
        }

        public static object ToSerializableErrorResponse(object response)
        {
            var errorResult = response as IHttpResult;
            if (errorResult != null)
                return errorResult.Response;

            var ex = response as Exception;
            return ex != null ? ex.ToResponseStatus() : null;
        }
    }
}