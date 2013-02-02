using System;
using System.Collections.Generic;

namespace ServiceStack.ServiceInterface.ServiceModel
{
	/// <summary>
	/// A log entry added by the IRequestLogger
	/// </summary>
	public class RequestLogEntry
	{
		public long Id { get; set; }
		public DateTime DateTime { get; set; }
		public string HttpMethod { get; set; }
		public string AbsoluteUri { get; set; }
		public string PathInfo { get; set; }
        public string RequestBody { get; set; }
        public object RequestDto { get; set; }
        public string UserAuthId { get; set; }
		public string SessionId { get; set; }
		public string IpAddress { get; set; }
		public string ForwardedFor { get; set; }
		public string Referer { get; set; }
		public Dictionary<string, string> Headers { get; set; }
		public Dictionary<string, string> FormData { get; set; }
		public Dictionary<string, object> Items { get; set; }
		public object Session { get; set; }
		public object ResponseDto { get; set; }
		public object ErrorResponse { get; set; }
        public TimeSpan RequestDuration { get; set; }
	}
}