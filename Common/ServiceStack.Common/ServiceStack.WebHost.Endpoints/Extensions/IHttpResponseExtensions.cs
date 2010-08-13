using System;
using System.IO;
using System.Text;
using ServiceStack.Common.Extensions;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Service;
using ServiceStack.Text;
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

				foreach (var globalResponseHeader in EndpointHost.Config.GlobalResponseHeaders)
				{
					response.AddHeader(globalResponseHeader.Key, globalResponseHeader.Value);
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

						if (ContentType.HeaderContentType.Equals(responseHeaders.Key, StringComparison.InvariantCultureIgnoreCase))
						{
							response.ContentType = responseHeaders.Value;
						}
						else
						{							
							Log.DebugFormat("Setting Custom HTTP Header: {0}: {1}", responseHeaders.Key, responseHeaders.Value);
							response.AddHeader(responseHeaders.Key, responseHeaders.Value);
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

				response.WriteErrorToResponse(defaultContentType, operationName, errorMessage, ex);
				return true;
			}
			finally
			{
				//Both seem to throw an exception??
				//Do not use response.Close(); does not have the same effect
				//response.End();
			}
		}

		public static void WriteTextToResponse(this IHttpResponse response, string text, string defaultContentType)
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

		public static void WriteErrorToResponse(this IHttpResponse response, string contentType,
			string operationName, string errorMessage, Exception ex)
		{
			switch (contentType)
			{
				case ContentType.Xml:
					WriteXmlErrorToResponse(response, operationName, errorMessage, ex);
					break;
				case ContentType.Json:
					WriteJsonErrorToResponse(response, operationName, errorMessage, ex);
					break;
				case ContentType.Jsv:
					WriteJsvErrorToResponse(response, operationName, errorMessage, ex);
					break;
				default:
					WriteXmlErrorToResponse(response, operationName, errorMessage, ex);
					break;
			}
		}

		public static void WriteXmlErrorToResponse(this IHttpResponse response,
			string operationName, string errorMessage, Exception ex)
		{
			var sb = new StringBuilder();
			sb.AppendFormat("<{0}Response xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"{1}\">\n",
				operationName, EndpointHost.Config.DefaultOperationNamespace);
			sb.AppendLine("<ResponseStatus>");
			sb.AppendFormat("<ErrorCode>{0}</ErrorCode>\n", ex.GetType().Name.EncodeXml());
			sb.AppendFormat("<Message>{0}</Message>\n", ex.Message.EncodeXml());
			sb.AppendFormat("<StackTrace>{0}</StackTrace>\n", ex.StackTrace.EncodeXml());
			sb.AppendLine("</ResponseStatus>");
			sb.AppendFormat("</{0}Response>", operationName);

			response.StatusCode = 500;
			WriteTextToResponse(response, sb.ToString(), ContentType.Xml);
		}

		public static void WriteJsonErrorToResponse(this IHttpResponse response,
			string operationName, string errorMessage, Exception ex)
		{
			var sb = new StringBuilder();
			sb.AppendLine("{");
			sb.AppendLine("\"ResponseStatus\":{");
			sb.AppendFormat(" \"ErrorCode\":{0},\n", ex.GetType().Name.EncodeJson());
			sb.AppendFormat(" \"Message\":{0},\n", ex.Message.EncodeJson());
			sb.AppendFormat(" \"StackTrace\":{0}\n", ex.StackTrace.EncodeJson());
			sb.AppendLine("}");
			sb.AppendLine("}");

			response.StatusCode = 500;
			WriteTextToResponse(response, sb.ToString(), ContentType.Json);
		}

		public static void WriteJsvErrorToResponse(this IHttpResponse response,
			string operationName, string errorMessage, Exception ex)
		{
			var sb = new StringBuilder();
			sb.Append("{");
			sb.Append("ResponseStatus:{");
			sb.AppendFormat("ErrorCode:{0},", ex.GetType().Name.EncodeJsv());
			sb.AppendFormat("Message:{0},", ex.Message.EncodeJsv());
			sb.AppendFormat("StackTrace:{0}", ex.StackTrace.EncodeJsv());
			sb.Append("}");
			sb.Append("}");

			response.StatusCode = 500;
			WriteTextToResponse(response, sb.ToString(), ContentType.Jsv);
		}

	}
}