//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections;
using System.Collections.Generic;
using ServiceStack.DataAnnotations;

namespace ServiceStack
{
    /// <summary>
    /// A log entry added by the IRequestLogger
    /// </summary>
    public class RequestLogEntry : IMeta
    {
        [AutoIncrement]
        public long Id { get; set; }
        public DateTime DateTime { get; set; }
        public int StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public string HttpMethod { get; set; }
        public string AbsoluteUri { get; set; }
        public string PathInfo { get; set; }
        [StringLength(StringLengthAttribute.MaxText)]
        public string RequestBody { get; set; }
        public object RequestDto { get; set; }
        public string UserAuthId { get; set; }
        public string SessionId { get; set; }
        public string IpAddress { get; set; }
        public string ForwardedFor { get; set; }
        public string Referer { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Dictionary<string, string> FormData { get; set; }
        public Dictionary<string, string> Items { get; set; }
        public object Session { get; set; }
        public object ResponseDto { get; set; }
        public object ErrorResponse { get; set; }
        public string ExceptionSource { get; set; }
        public IDictionary ExceptionData { get; set; }
        public TimeSpan RequestDuration { get; set; }
        public Dictionary<string, string> Meta { get; set; }
    }
}