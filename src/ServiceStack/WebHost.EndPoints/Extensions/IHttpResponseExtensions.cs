using System;
using System.IO;
using System.Text;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.Service;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
	public static class HttpResponseExtensions
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(HttpResponseExtensions));

		public static bool WriteToOutputStream(IHttpResponse response, object result)
		{
			//var responseStream = response.OutputStream;

			var streamWriter = result as IStreamWriter;
			if (streamWriter != null)
			{
				streamWriter.WriteTo(response.OutputStream);
				return true;
			}

			var stream = result as Stream;
			if (stream != null)
			{
				stream.WriteTo(response.OutputStream);
				return true;
			}

			var bytes = result as byte[];
			if (bytes != null)
			{
				response.ContentType = ContentType.Binary;
				response.OutputStream.Write(bytes, 0, bytes.Length);
				return true;
			}

			return false;
		}

		public static bool WriteToResponse(this IHttpResponse httpRes, object result, string contentType)
		{
			var serializer = EndpointHost.AppHost.ContentTypeFilters.GetResponseSerializer(contentType);
			return httpRes.WriteToResponse(result, serializer, new SerializationContext(contentType));
		}

		public static bool WriteToResponse(this IHttpResponse httpRes, IHttpRequest httpReq, object result)
		{
			return WriteToResponse(httpRes, httpReq, result, null, null);
		}

		public static bool WriteToResponse(this IHttpResponse httpRes, IHttpRequest httpReq, object result, byte[] bodyPrefix, byte[] bodySuffix)
		{
			if (result == null) return true;

			var serializationContext = new HttpRequestContext(httpReq, result);
			var httpResult = result as IHttpResult;
			if (httpResult != null)
			{
				if (httpResult.ResponseFilter == null)
				{
					httpResult.ResponseFilter = EndpointHost.AppHost.ContentTypeFilters;
				}
				httpResult.RequestContext = serializationContext;
				var httpResSerializer = httpResult.ResponseFilter.GetResponseSerializer(httpReq.ResponseContentType);
				return httpRes.WriteToResponse(httpResult, httpResSerializer, serializationContext, bodyPrefix, bodySuffix);
			}

			var serializer = EndpointHost.AppHost.ContentTypeFilters.GetResponseSerializer(httpReq.ResponseContentType);
			return httpRes.WriteToResponse(result, serializer, serializationContext, bodyPrefix, bodySuffix);
		}

		public static bool WriteToResponse(this IHttpResponse httpRes, object result, ResponseSerializerDelegate serializer, IRequestContext serializationContext)
		{
			return httpRes.WriteToResponse(result, serializer, serializationContext, null, null);
		}

		/// <summary>
		/// Writes to response.
		/// Response headers are customizable by implementing IHasOptions an returning Dictionary of Http headers.
		/// </summary>
		/// <param name="response">The response.</param>
		/// <param name="result">Whether or not it was implicity handled by ServiceStack's built-in handlers.</param>
		/// <param name="defaultAction">The default action.</param>
		/// <param name="serializerCtx">The serialization context.</param>
		/// <param name="bodyPrefix">Add prefix to response body if any</param>
		/// <param name="bodySuffix">Add suffix to response body if any</param>
		/// <returns></returns>
		public static bool WriteToResponse(this IHttpResponse response, object result, ResponseSerializerDelegate defaultAction, IRequestContext serializerCtx, byte[] bodyPrefix, byte[] bodySuffix)
		{
			var defaultContentType = serializerCtx.ResponseContentType;
			try
			{
				if (result == null) return true;

				foreach (var globalResponseHeader in EndpointHost.Config.GlobalResponseHeaders)
				{
					response.AddHeader(globalResponseHeader.Key, globalResponseHeader.Value);
				}

				var httpResult = result as IHttpResult;
				if (httpResult != null)
				{
					response.StatusCode = (int)httpResult.StatusCode;
					response.StatusDescription = httpResult.StatusDescription ?? httpResult.StatusCode.ToString();
					if (string.IsNullOrEmpty(httpResult.ContentType))
					{
						httpResult.ContentType = defaultContentType;
					}
					response.ContentType = httpResult.ContentType;
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

						Log.DebugFormat("Setting Custom HTTP Header: {0}: {1}", responseHeaders.Key, responseHeaders.Value);
						response.AddHeader(responseHeaders.Key, responseHeaders.Value);
					}
				}

				if (WriteToOutputStream(response, result))
				{
					return true;
				}

				if (httpResult != null)
				{
					result = httpResult.Response;
				}

				//ContentType='text/html' is the default for a HttpResponse
				//Do not override if another has been set
				if (response.ContentType == null || response.ContentType == ContentType.Html)
				{
					response.ContentType = defaultContentType;
				}

				var responseText = result as string;
				if (responseText != null)
				{
					if (bodyPrefix != null) response.OutputStream.Write(bodyPrefix, 0, bodyPrefix.Length);
					WriteTextToResponse(response, responseText, defaultContentType);
					if (bodySuffix != null) response.OutputStream.Write(bodySuffix, 0, bodySuffix.Length);
					return true;
				}

				if (defaultAction == null)
				{
					throw new ArgumentNullException("defaultAction", string.Format(
						"As result '{0}' is not a supported responseType, a defaultAction must be supplied",
						result.GetType().Name));
				}

				if (bodyPrefix != null) response.OutputStream.Write(bodyPrefix, 0, bodyPrefix.Length);
				defaultAction(serializerCtx, result, response);
				if (bodySuffix != null) response.OutputStream.Write(bodySuffix, 0, bodySuffix.Length);

				return false;
			}
			catch (Exception ex)
			{
                //TM: It would be good to handle 'remote end dropped connection' problems here. Arguably they should at least be suppressible via configuration

                //default value 'true' to be consistent with the way SS worked before this change
                bool writeErrorToResponse = ServiceStack.Configuration.ConfigUtils.GetAppSetting<bool>(ServiceStack.Configuration.Keys.WriteErrorsToResponse, true);

                if(!writeErrorToResponse) {
                    throw;
                }
                var errorMessage = string.Format("Error occured while Processing Request: [{0}] {1}",
                    ex.GetType().Name, ex.Message);
                Log.Error(errorMessage, ex);

                var operationName = result != null
                    ? result.GetType().Name.Replace("Response", "")
                    : "OperationName";

                try {
                    if(!response.IsClosed) {
                        response.WriteErrorToResponse(defaultContentType, operationName, errorMessage, ex);
                    }
                }
                catch(Exception WriteErrorEx) {
                    //Exception in writing to response should not hide the original exception
                    Log.Info("Failed to write error to response: {0}", WriteErrorEx);
                    //rethrow the original exception
                    throw ex;
                }
                return true;
            }
			finally
			{
				response.Close();
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

		public static void WriteErrorToResponse(this IHttpResponse response,
			EndpointAttributes contentType, string operationName, string errorMessage, Exception ex)
		{
			switch (contentType)
			{
				case EndpointAttributes.Xml:
					WriteXmlErrorToResponse(response, operationName, errorMessage, ex);
					break;

				case EndpointAttributes.Json:
					WriteJsonErrorToResponse(response, operationName, errorMessage, ex);
					break;

				case EndpointAttributes.Jsv:
					WriteJsvErrorToResponse(response, operationName, errorMessage, ex);
					break;

				default:
					WriteXmlErrorToResponse(response, operationName, errorMessage, ex);
					break;
			}
		}

		private static void WriteErrorTextToResponse(this IHttpResponse response, StringBuilder sb, string contentType)
		{
			response.StatusCode = 500;
			WriteTextToResponse(response, sb.ToString(), contentType);
			response.Close();
		}

		private static void WriteXmlErrorToResponse(this IHttpResponse response,
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

			response.WriteErrorTextToResponse(sb, ContentType.Xml);
		}

		private static void WriteJsonErrorToResponse(this IHttpResponse response,
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

			response.WriteErrorTextToResponse(sb, ContentType.Json);
		}

		private static void WriteJsvErrorToResponse(this IHttpResponse response,
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

			response.WriteErrorTextToResponse(sb, ContentType.Jsv);
		}

	}
}
