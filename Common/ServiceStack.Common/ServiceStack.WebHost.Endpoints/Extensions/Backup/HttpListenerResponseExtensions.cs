using System;
using System.IO;
using System.Net;
using System.Web;
using ServiceStack.Common.Extensions;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Service;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints.Extensions.Backup
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
		/// 
		/// Response headers are customizable by implementing IHasOptions an returning Dictionary of Http headers.
		/// 
		/// If its not handled by any of the above it will call the defaultAction Func provided and write the xml output to the response
		/// </summary>
		/// <param name="response">The response.</param>
		/// <param name="result">The result.</param>
		/// <param name="defaultAction">The default action.</param>
		/// <param name="defaultContentType">Default type of the content.</param>
		/// <returns></returns>
		public static bool WriteToResponse(this HttpListenerResponse response, object result, Func<object, string> defaultAction, string defaultContentType)
		{
			try
			{
				if (result == null)
				{
					return true;
				}

				var responseOptions = result as IHasOptions;
				if (responseOptions != null)
				{
					//Reserving options with keys in the format 'xx.xxx' (No Http headers contain a '.' so its a safe restriction)
					const string reservedOptions = ".";

					foreach (var responseHeaders in responseOptions.Options)
					{
						if (responseHeaders.Key.Contains(reservedOptions)) continue;

						response.Headers[responseHeaders.Key] = responseHeaders.Value;
					}
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
			catch (Exception ex)
			{
				var errorMessage = string.Format("Error occured while Processing Request: {0}", ex.Message);
				Log.Error(errorMessage, ex);
				response.WriteErrorToResponse(errorMessage, ex);
				return true;
			}
			finally
			{
				//There is no response.End();
				response.Close();
			}
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
		}

		public static void WriteErrorToResponse(this HttpListenerResponse response, string errorMessage, Exception ex)
		{
			var responseXml = string.Format("<Error>\n\t<Message>{0}</Message>\n\t<StackTrace>\n\t\t{1}\n\t</StackTrace>\n</Error>",
				errorMessage, ex.StackTrace);

			WriteTextToResponse(response, responseXml, ContentType.XmlText);
		}
	}
}