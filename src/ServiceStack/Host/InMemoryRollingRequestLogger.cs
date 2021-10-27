using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public class InMemoryRollingRequestLogger : IRequestLogger
    {
        internal static long requestId = 0;

        public const int DefaultCapacity = 1000;
        protected readonly ConcurrentQueue<RequestLogEntry> logEntries = new ConcurrentQueue<RequestLogEntry>();
        protected readonly int capacity;

        public bool EnableSessionTracking { get; set; }

        public bool EnableRequestBodyTracking { get; set; }

        public bool EnableResponseTracking { get; set; }

        public bool EnableErrorTracking { get; set; }

        public bool LimitToServiceRequests { get; set; }

        public string[] RequiredRoles { get; set; }

        public Func<IRequest, bool> SkipLogging { get; set; }
        
        public Type[] ExcludeRequestDtoTypes { get; set; }

        public Type[] HideRequestBodyForRequestDtoTypes { get; set; }

        public Action<IRequest, RequestLogEntry> RequestLogFilter { get; set; }

        public Func<object,bool> IgnoreFilter { get; set; } 

        public Func<DateTime> CurrentDateFn { get; set; } = () => DateTime.UtcNow;

        protected InMemoryRollingRequestLogger() {}

        public InMemoryRollingRequestLogger(int? capacity = DefaultCapacity)
        {
            this.capacity = capacity.GetValueOrDefault(DefaultCapacity);
        }

        public virtual bool ShouldSkip(IRequest req, object requestDto)
        {
            var dto = requestDto ?? req.Dto;
            if (LimitToServiceRequests && dto == null)
                return true;

            var requestType = dto?.GetType();

            return ExcludeRequestType(requestType) || SkipLogging?.Invoke(req) == true;
        }

        public virtual void Log(IRequest request, object requestDto, object response, TimeSpan requestDuration)
        {
            if (ShouldSkip(request, requestDto))
                return;

            var requestType = requestDto?.GetType();

            var entry = CreateEntry(request, requestDto, response, requestDuration, requestType);

            RequestLogFilter?.Invoke(request, entry);

            if (IgnoreFilter != null)
            {
                if (entry.RequestDto != null && IgnoreFilter(entry.RequestDto))
                    entry.RequestDto = null;
                if (entry.ResponseDto != null && IgnoreFilter(entry.ResponseDto))
                    entry.ResponseDto = null;
                if (entry.Session != null && IgnoreFilter(entry.Session))
                    entry.Session = null;
                if (entry.ErrorResponse != null && IgnoreFilter(entry.ErrorResponse))
                    entry.ErrorResponse = null;
                if (entry.ExceptionData != null)
                {
                    List<object> keysToRemove = null;
                    foreach (var key in entry.ExceptionData.Keys)
                    {
                        var val = entry.ExceptionData[key];
                        if (val != null && IgnoreFilter(val))
                        {
                            keysToRemove ??= new List<object>();
                            keysToRemove.Add(key);
                        }
                    }
                    keysToRemove?.Each(entry.ExceptionData.Remove);
                }
            }

            logEntries.Enqueue(entry);

            RequestLogEntry dummy;
            if (logEntries.Count > capacity)
                logEntries.TryDequeue(out dummy);
        }

        protected RequestLogEntry CreateEntry(IRequest request, object requestDto, object response, TimeSpan requestDuration, Type requestType)
        {
            var entry = new RequestLogEntry
            {
                Id = Interlocked.Increment(ref requestId),
                DateTime = CurrentDateFn(),
                RequestDuration = requestDuration,
            };

            if (request != null)
            {
                entry.HttpMethod = request.Verb;
                entry.AbsoluteUri = request.AbsoluteUri;
                entry.PathInfo = request.PathInfo;
                entry.IpAddress = request.UserHostAddress;
                entry.ForwardedFor = request.Headers[HttpHeaders.XForwardedFor];
                entry.Referer = request.Headers[HttpHeaders.Referer];
                entry.Headers = request.Headers.ToDictionary();
                entry.UserAuthId = request.GetItemStringValue(HttpHeaders.XUserAuthId);
                entry.Items = SerializableItems(request.Items);
                entry.Session = EnableSessionTracking ? request.GetSession() : null;
                entry.StatusCode = request.Response.StatusCode;
                entry.StatusDescription = request.Response.StatusDescription;

                var isClosed = request.Response.IsClosed;
                if (!isClosed)
                {
                    entry.UserAuthId = request.GetItemOrCookie(HttpHeaders.XUserAuthId);
                    entry.SessionId = request.GetSessionId();
                }

                if (HideRequestBodyForRequestDtoTypes != null
                    && requestType != null
                    && !HideRequestBodyForRequestDtoTypes.Contains(requestType))
                {
                    entry.RequestDto = requestDto;

                    if (!isClosed)
                        entry.FormData = request.FormData.ToDictionary();

                    if (EnableRequestBodyTracking && request.CanReadRequestBody())
                    {
#if NETCORE
                        // https://forums.servicestack.net/t/unexpected-end-of-stream-when-uploading-to-aspnet-core/6478/6
                        if (!request.ContentType.MatchesContentType(MimeTypes.MultiPartFormData))
                        {
                            entry.RequestBody = request.GetRawBody();
                        }
#else
                        entry.RequestBody = request.GetRawBody();
#endif
                    }
                }
            }

            if (!response.IsErrorResponse())
            {
                if (EnableResponseTracking)
                    entry.ResponseDto = response.GetResponseDto();
            }
            else
            {
                if (EnableErrorTracking)
                {
                    entry.ErrorResponse = ToSerializableErrorResponse(response);

                    if (response is IHttpError httpError)
                    {
                        entry.StatusCode = (int)httpError.StatusCode;
                        entry.StatusDescription = httpError.StatusDescription;
                    }

                    if (response is Exception exception)
                    {
                        if (exception.InnerException != null)
                            exception = exception.InnerException;
                        
                        entry.ExceptionSource = exception.Source;
                        entry.ExceptionData = exception.Data;
                    }
                }
            }

            return entry;
        }

        protected bool ExcludeRequestType(Type requestType)
        {
            return ExcludeRequestDtoTypes != null
                   && requestType != null
                   && ExcludeRequestDtoTypes.Contains(requestType);
        }

        public Dictionary<string, string> SerializableItems(Dictionary<string, object> items)
        {
            var to = new Dictionary<string, string>();
            foreach (var item in items)
            {
                var value = item.Value?.ToString() ?? "(null)";
                to[item.Key] = value;
            }

            return to;
        }

        public virtual List<RequestLogEntry> GetLatestLogs(int? take)
        {
            var requestLogEntries = logEntries.ToArray();
            return take.HasValue
                ? new List<RequestLogEntry>(requestLogEntries.Take(take.Value))
                : new List<RequestLogEntry>(requestLogEntries);
        }

        public static object ToSerializableErrorResponse(object response)
        {
            if (response is IHttpResult errorResult)
                return errorResult.Response;
            else if (response is ErrorResponse errorResponse)
                return errorResponse.GetResponseDto();

            var ex = response as Exception;
            return ex?.ToResponseStatus();
        }
    }
}