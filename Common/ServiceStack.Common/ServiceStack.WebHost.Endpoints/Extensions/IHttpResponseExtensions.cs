using System;
using System.IO;
using System.Text;
using ServiceStack.Common.Extensions;
using ServiceStack.Configuration;
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
		public static bool WriteToResponse(this IHttpResponse response, object result, string defaultContentType)
		{
			return WriteToResponse(response, result, null, defaultContentType);
		}

		/// <summary>
		/// Writes to response.
		/// 
		/// Response headers are customizable by implementing IHasOptions an returning Dictionary of Http headers.
		/// 
		/// </summary>
		/// <param name="response">The response.</param>
		/// <param name="result">Whether or not it was implicity handled by ServiceStack's built-in handlers.</param>
		/// <param name="defaultAction">The default action.</param>
		/// <param name="defaultContentType">Default response ContentType.</param>
		/// <returns></returns>
		public static bool WriteToResponse(this IHttpResponse response, object result, Func<object, string> defaultAction, string defaultContentType)
		{
			try
			{
				if (result == null)
				{
					return true;
				}

				/* Mono Error: Exception: Method not found: 'System.Web.HttpResponse.get_Headers' */
				var responseOptions = result as IHasOptions;
				if (responseOptions != null)
				{
					//Reserving options with keys in the format 'xx.xxx' (No Http headers contain a '.' so its a safe restriction)
					const string reservedOptions = ".";

					foreach (var responseHeaders in responseOptions.Options)
					{
						if (responseHeaders.Key.Contains(reservedOptions)) continue;

						if (responseHeaders.Key == ContentType.HeaderContentType)
						{
							response.ContentType = responseHeaders.Value;
						}
						else
						{
							response.Headers[responseHeaders.Key] = responseHeaders.Value;
						}
					}
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
			catch (Exception ex)
			{
				var errorMessage = string.Format("Error occured while Processing Request: {0}", ex.Message);
				Log.Error(errorMessage, ex);

				var operationName = result != null
					? result.GetType().Name.Replace("Response", "")
					: "OperationName";

				response.WriteErrorToResponse(operationName, errorMessage, ex);
				return true;
			}
			finally
			{
				//Both seem to throw an exception??
				//Do not use response.Close(); does not have the same effect
				//response.End();
			}
		}

		public static void WriteTextToResponse(IHttpResponse response, string text, string defaultContentType)
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

		public static void WriteErrorToResponse(this IHttpResponse response,
			string operationName, string errorMessage, Exception ex)
		{
			var sb = new StringBuilder();
			sb.AppendFormat("<{0}Response>\n", operationName);
			sb.AppendLine("<ResponseStatus>");
			sb.AppendFormat("<ErrorCode>{0}</ErrorCode>\n", ex.GetType().Name);
			sb.AppendFormat("<ErrorMessage>{0}</ErrorMessage>\n", ex.Message.EncodeXml());
			sb.AppendFormat("<StackTrace>{0}</StackTrace>\n", ex.StackTrace.EncodeXml());
			sb.AppendLine("</ResponseStatus>");
			sb.AppendFormat("</{0}Response>", operationName);

			WriteTextToResponse(response, sb.ToString(), ContentType.XmlText);
		}

	}
}