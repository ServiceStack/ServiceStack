using System;
using System.IO;
using System.Net;
using System.Web;
using ServiceStack.Common.Extensions;
using ServiceStack.Logging;
using ServiceStack.Service;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
	public static class HttpResponseExtensions
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(HttpResponseExtensions));

		public static bool WriteToOutputStream(Stream responseStream, object result)
		{
			var streamWriter = result as IStreamWriter;
			if (streamWriter != null)
			{
				streamWriter.WriteTo(responseStream);
				return true;
			}

			var stream = result as Stream;
			if (stream != null)
			{
				stream.WriteTo(responseStream);
				return true;
			}

			return false;
		}


		/// <summary>
		/// Writes to response.
		/// </summary>
		/// <param name="response">The response.</param>
		/// <param name="result">Whether or not it was implicity handled by ServiceStack's built-in handlers.</param>
		/// <param name="defaultContentType">Default response ContentType.</param>
		/// <returns></returns>
		public static bool WriteToResponse(this HttpResponse response, object result, string defaultContentType)
		{
			return WriteToResponse(response, result, null, defaultContentType);
		}

		/// <summary>
		/// Writes to response.
		/// </summary>
		/// <param name="response">The response.</param>
		/// <param name="result">Whether or not it was implicity handled by ServiceStack's built-in handlers.</param>
		/// <param name="defaultAction">The default action.</param>
		/// <param name="defaultContentType">Default response ContentType.</param>
		/// <returns></returns>
		public static bool WriteToResponse(this HttpResponse response, object result, Func<object, string> defaultAction, string defaultContentType)
		{
			try
			{
				if (result == null)
				{
					return true;
				}

				if (WriteToOutputStream(response.OutputStream, result))
				{
					return true;
				}

				var responseText = result as string;
				if (responseText != null)
				{
					WriteTextToResponse(response, responseText, defaultContentType);
					return true;
				}

				if (defaultAction == null)
				{
					throw new ArgumentNullException("defaultAction", string.Format(
						"As result '{0}' is not a supported responseType, a defaultAction must be supplied",
						result.GetType().Name));
				}

				WriteTextToResponse(response, defaultAction(result), defaultContentType);
				return false;

			}
			finally 
			{
				//Do not use response.Close(); does not have the same effect
				response.End();
			}
		}

		public static void WriteTextToResponse(HttpResponse response, string text, string defaultContentType)
		{
			try
			{
				//ContentType='text/html' is the default for a HttpResponse
				//Do not override if another has been set
				if (response.ContentType == null || response.ContentType == ContentType.Html)
				{
					response.ContentType = defaultContentType;
				}
				response.Write(text);
			}
			catch (Exception ex)
			{
				Log.Error("Could not WriteTextToResponse: " + ex.Message, ex);
				throw;
			}
		}
        
	}
}