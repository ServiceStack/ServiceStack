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
        protected readonly ConcurrentQueue<RequestLogEntry> logEntries = new();
        protected readonly int capacity;

        public bool EnableSessionTracking { get; set; }

        public bool EnableRequestBodyTracking { get; set; }
        public Func<IRequest, bool> RequestBodyTrackingFilter { get; set; }

        public bool EnableResponseTracking { get; set; }
        public Func<IRequest, bool> ResponseTrackingFilter { get; set; }

        public bool EnableErrorTracking { get; set; }

        public bool LimitToServiceRequests { get; set; }

        public string[] RequiredRoles { get; set; }

        public Func<IRequest, bool> SkipLogging { get; set; }
        
        public Type[] ExcludeRequestDtoTypes { get; set; }

        public Type[] HideRequestBodyForRequestDtoTypes { get; set; }
        
        public Type[] ExcludeResponseTypes { get; set; }

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

            if (logEntries.Count > capacity)
                logEntries.TryDequeue(out _);
        }

        protected RequestLogEntry CreateEntry(IRequest request, object requestDto, object response, TimeSpan requestDuration, Type requestType)
        {
            var entry = new RequestLogEntry
            {
                Id = Interlocked.Increment(ref requestId),
                TraceId = request.GetTraceId(),
                OperationName = request.OperationName,
                DateTime = CurrentDateFn(),
                RequestDuration = requestDuration,
                HttpMethod = request.Verb,
                AbsoluteUri = request.AbsoluteUri,
                PathInfo = request.PathInfo,
                IpAddress = request.UserHostAddress,
                ForwardedFor = request.Headers[HttpHeaders.XForwardedFor],
                Referer = request.Headers[HttpHeaders.Referer],
                Headers = request.Headers.ToDictionary(),
                UserAuthId = request.GetItemStringValue(HttpHeaders.XUserAuthId),
                Items = SerializableItems(request.Items),
                Session = EnableSessionTracking ? request.GetSession() : null,
                StatusCode = request.Response.StatusCode,
                StatusDescription = request.Response.StatusDescription
            };

            if (request.Response is IHasHeaders hasHeaders)
                entry.ResponseHeaders = hasHeaders.Headers;

            var isClosed = request.Response.IsClosed;
            if (!isClosed)
            {
                entry.UserAuthId = request.GetItemOrCookie(HttpHeaders.XUserAuthId);
                entry.SessionId = request.GetSessionId();
            }

            if (HideRequestBodyForRequestDtoTypes != null
                && requestType != null
                && !HideRequestBodyForRequestDtoTypes.Any(x => x.IsAssignableFrom(requestType)))
            {
                entry.RequestDto = requestDto;

                if (!isClosed)
                    entry.FormData = request.FormData.ToDictionary();

                var enableRequestBodyTracking = RequestBodyTrackingFilter?.Invoke(request);
                if (enableRequestBodyTracking ?? EnableRequestBodyTracking && request.CanReadRequestBody())
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
                
            if (!response.IsErrorResponse())
            {
                var enableResponseTracking = ResponseTrackingFilter?.Invoke(request);
                if (enableResponseTracking ?? EnableResponseTracking)
                {
                    var responseDto = response.GetResponseDto();
                    if (responseDto != null && !ExcludeResponseTypes.Any(x => x.IsInstanceOfType(responseDto)))
                    {
                        entry.ResponseDto = responseDto;
                    }
                }
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
                   && ExcludeRequestDtoTypes.Any(x => x.IsAssignableFrom(requestType));
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
            return take.HasValue
                ? new List<RequestLogEntry>(logEntries.Take(take.Value))
                : new List<RequestLogEntry>(logEntries);
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