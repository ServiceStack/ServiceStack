using System;
using System.IO;
using System.Net;
using System.Web;
using ServiceStack.Common.Extensions;
using ServiceStack.Logging;
using ServiceStack.Service;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
	public static class HttpListenerResponseExtensions
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(HttpListenerResponseExtensions));

		public static bool WriteToResponse(this HttpListenerResponse response, object result, string defaultContentType)
		{
			return WriteToResponse(response, result, null, defaultContentType);
		}

		/// <summary>
		/// Writes the response of a PortHandler to the ResponseStream.
		/// - Handles, strings (returns as xml)
		/// - Stream/MemoryStream
		/// - IStreamWriter result
		/// If its not handled by any of the above it will call the defaultAction Func provided and write the xml output to the response
		/// </summary>
		/// <param name="response">The response.</param>
		/// <param name="result">The result.</param>
		/// <param name="defaultAction">The default action.</param>
		/// <param name="defaultContentType">Default type of the content.</param>
		/// <returns></returns>
		public static bool WriteToResponse(this HttpListenerResponse response, object result, Func<object, string> defaultAction, string defaultContentType)
		{
			if (result == null)
			{
				response.Close();
				return true;
			}

			if (HttpResponseExtensions.WriteToOutputStream(response.OutputStream, result)) return true;

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

		public static void WriteTextToResponse(HttpListenerResponse response, string stringResult, string defaultContentType)
		{
			try
			{
				var bOutput = System.Text.Encoding.UTF8.GetBytes(stringResult);

				if (response.ContentType == null)
				{
					response.ContentType = defaultContentType;
				}
				response.ContentLength64 = bOutput.Length;

				var outputStream = response.OutputStream;
				outputStream.Write(bOutput, 0, bOutput.Length);
				outputStream.Close();

			}
			catch (Exception ex)
			{
				Log.Error("Could not WriteTextToResponse: " + ex.Message, ex);
				throw;
			}
			finally
			{
				response.Close();
			}
		}


	}
}