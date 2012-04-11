using System;
using ServiceStack.Common.Utils;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.Common.Web
{
	public static class HttpResultExtensions
	{
		/// <summary>
		/// Shortcut to get the ResponseDTO whether it's bare or inside a IHttpResult
		/// </summary>
		/// <param name="response"></param>
		/// <returns></returns>
		public static object ToResponseDto(this object response)
		{
			if (response == null) return null;
			var httpResult = response as IHttpResult;
			return httpResult != null ? httpResult.Response : response;
		}

		/// <summary>
		/// Shortcut to get the ResponseStatus whether it's bare or inside a IHttpResult
		/// </summary>
		/// <param name="response"></param>
		/// <returns></returns>
		public static ResponseStatus ToResponseStatus(this object response)
		{
			if (response == null) return null;

			var hasResponseStatus = response as IHasResponseStatus;
			if (hasResponseStatus != null)
				return hasResponseStatus.ResponseStatus;

			var propertyInfo = response.GetType().GetProperty("ResponseStatus");
			if (propertyInfo == null)
				return null;

			return ReflectionUtils.GetProperty(response, propertyInfo) as ResponseStatus;
		}

		/// <summary>
		/// Whether the response is an IHttpError or Exception
		/// </summary>
		/// <param name="response"></param>
		/// <returns></returns>
		public static bool IsErrorResponse(this object response)
		{
			return response != null && (response is IHttpError || response is Exception);
		}
	}
}