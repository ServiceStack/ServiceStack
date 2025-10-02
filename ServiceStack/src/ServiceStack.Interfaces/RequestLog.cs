#nullable enable

using System;
using System.Collections.Generic;
using ServiceStack;
using ServiceStack.DataAnnotations;

/// <summary>
/// Request Log Data Model for (SqliteRequestLogger, DbLogger)
/// </summary>
public class RequestLog : IMeta
{
    [AutoIncrement]
    public long Id { get; set; }
    public string TraceId { get; set; }
    public string OperationName { get; set; }
    [Index]
    public DateTime DateTime { get; set; }
    public int StatusCode { get; set; }
    public string? StatusDescription { get; set; }
    public string? HttpMethod { get; set; }
    public string? AbsoluteUri { get; set; }
    public string? PathInfo { get; set; }
    public string? Request { get; set; }
    [StringLength(StringLengthAttribute.MaxText)]
    public string? RequestBody { get; set; }
    public string? UserAuthId { get; set; }
    public string? SessionId { get; set; }
    public string? IpAddress { get; set; }
    public string? ForwardedFor { get; set; }
    public string? Referer { get; set; }
    public Dictionary<string, string> Headers { get; set; } = [];
    public Dictionary<string, string>? FormData { get; set; }
    public Dictionary<string, string> Items { get; set; } = [];
    public Dictionary<string, string>? ResponseHeaders { get; set; }
    public string? Response { get; set; }
    [StringLength(StringLengthAttribute.MaxText)]
    public string? ResponseBody { get; set; }
    [StringLength(StringLengthAttribute.MaxText)]
    public string? SessionBody { get; set; }
    public ResponseStatus? Error { get; set; }
    public string? ExceptionSource { get; set; }
    public string? ExceptionDataBody { get; set; }
    public TimeSpan RequestDuration { get; set; }
    public Dictionary<string, string>? Meta { get; set; }
}
