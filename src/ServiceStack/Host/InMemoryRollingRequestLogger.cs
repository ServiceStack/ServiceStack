﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using ServiceStack.Web;

namespace ServiceStack.Host
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

        public void Log(IRequest request, object requestDto, object response, TimeSpan requestDuration)
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

            if (request != null)
            {
                entry.HttpMethod = request.Verb;
                entry.AbsoluteUri = request.AbsoluteUri;
                entry.PathInfo = request.PathInfo;
                entry.IpAddress = request.UserHostAddress;
                entry.ForwardedFor = request.Headers[HttpHeaders.XForwardedFor];
                entry.Referer = request.Headers[HttpHeaders.Referer];
                entry.Headers = request.Headers.ToDictionary();
                entry.UserAuthId = request.GetItemOrCookie(HttpHeaders.XUserAuthId);
                entry.SessionId = request.GetSessionId();
                entry.Items = SerializableItems(request.Items);
                entry.Session = EnableSessionTracking ? request.GetSession() : null;
                new NameValueCollection().ToDictionary();
            }

            if (HideRequestBodyForRequestDtoTypes != null
                && requestType != null
                && !HideRequestBodyForRequestDtoTypes.Contains(requestType)) 
            {
                entry.RequestDto = requestDto;
                if (request != null)
                {
                    entry.FormData = request.FormData.ToDictionary();

                    if (EnableRequestBodyTracking)
                    {
                        entry.RequestBody = request.GetRawBody();
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

        public Dictionary<string, string> SerializableItems(Dictionary<string, object> items)
        {
            var to = new Dictionary<string, string>();
            foreach (var item in items)
            {
                var value = item.Value == null
                    ? "(null)"
                    : item.Value.ToString();
                
                to[item.Key] = value;
            }

            return to;
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