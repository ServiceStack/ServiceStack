using System;
using System.Collections.Generic;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.ServiceHost
{
	/// <summary>
	/// Log every service request
	/// </summary>
	public interface IRequestLogger
	{
		/// <summary>
		/// Turn On/Off Session Tracking
		/// </summary>
		bool EnableSessionTracking { get; set; }

        /// <summary>
        /// Turn On/Off Raw Request Body Tracking
        /// </summary>
        bool EnableRequestBodyTracking { get; set; }

		/// <summary>
		/// Turn On/Off Tracking of Responses
		/// </summary>
		bool EnableResponseTracking { get; set; }

		/// <summary>
		/// Turn On/Off Tracking of Exceptions
		/// </summary>
		bool EnableErrorTracking { get; set; }

		/// <summary>
		/// Limit access to /requestlogs service to role
		/// </summary>
		string[] RequiredRoles { get; set; }

		/// <summary>
		/// Don't log requests of these types.
		/// </summary>
		Type[] ExcludeRequestDtoTypes { get; set; }

		/// <summary>
		/// Don't log request bodys for services with sensitive information.
		/// By default Auth and Registration requests are hidden.
		/// </summary>
		Type[] HideRequestBodyForRequestDtoTypes { get; set; }

		/// <summary>
		/// Log a request
		/// </summary>
		/// <param name="requestContext">The RequestContext</param>
		/// <param name="requestDto">Request DTO</param>
		/// <param name="response">Response DTO or Exception</param>
		/// <param name="elapsed">How long did the Request take</param>
		void Log(IRequestContext requestContext, object requestDto, object response, TimeSpan elapsed);

		/// <summary>
		/// View the most recent logs
		/// </summary>
		/// <param name="take"></param>
		/// <returns></returns>
		List<RequestLogEntry> GetLatestLogs(int? take);
	}
}