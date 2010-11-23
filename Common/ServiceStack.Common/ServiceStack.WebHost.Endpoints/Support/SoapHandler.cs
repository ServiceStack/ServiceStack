using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using System.Web;
using System.Xml;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public class SoapHandler : EndpointHandlerBase, IOneWay, ISyncReply
	{
		public virtual EndpointAttributes SoapType
		{
			get { return EndpointAttributes.Soap12; }
		}

		public void SendOneWay(Message requestMsg)
		{
			var endpointAttributes = EndpointAttributes.AsyncOneWay | SoapType;

			ExecuteMessage(requestMsg, endpointAttributes);
		}

		public Message Send(Message requestMsg)
		{
			var endpointAttributes = EndpointAttributes.SyncReply | SoapType;

			return ExecuteMessage(requestMsg, endpointAttributes);
		}

		protected Message ExecuteMessage(Message requestMsg, EndpointAttributes endpointAttributes)
		{
			string requestXml;
			using (var reader = requestMsg.GetReaderAtBodyContents())
			{
				requestXml = reader.ReadOuterXml();
			}

			var requestType = GetRequestType(requestMsg, requestXml);
			try
			{
				var request = DataContractDeserializer.Instance.Parse(requestXml, requestType);
				var response = ExecuteService(request, endpointAttributes);

				return requestMsg.Headers.Action == null
					? Message.CreateMessage(requestMsg.Version, null, response)
					: Message.CreateMessage(requestMsg.Version, requestType.Name + "Response", response);
			}
			catch (Exception ex)
			{
				throw new SerializationException("3) Error trying to deserialize requestType: "
					+ requestType
					+ ", xml body: " + requestXml, ex);
			}
		}

		protected static Message GetRequestMessage(HttpContext context)
		{
			using (var sr = new StreamReader(context.Request.InputStream))
			{
				var requestXml = sr.ReadToEnd();

				var doc = new XmlDocument();
				doc.LoadXml(requestXml);

				var msg = Message.CreateMessage(new XmlNodeReader(doc), requestXml.Length,
					MessageVersion.Soap12WSAddressingAugust2004);

				return msg;
			}
		}

		protected Type GetRequestType(Message requestMsg, string xml)
		{
			var action = GetAction(requestMsg, xml);

			var operationType = EndpointHost.ServiceOperations.GetOperationType(action);
			AssertOperationExists(action, operationType);

			return operationType;
		}

		protected string GetAction(Message requestMsg, string xml)
		{
			var action = GetActionFromHttpContext();
			if (action != null) return action;

			if (requestMsg.Headers.Action != null)
			{
				return requestMsg.Headers.Action;
			}

			if (xml.StartsWith("<"))
			{
				return xml.Substring(1, xml.IndexOf(" "));
			}

			return null;
		}

		protected static string GetActionFromHttpContext()
		{
			var context = HttpContext.Current;
			return GetAction(context);
		}

		private static string GetAction(HttpContext context)
		{
			if (context != null)
			{
				var contentType = context.Request.ContentType;
				return GetOperationName(contentType);
			}

			return null;
		}

		private static string GetOperationName(string contentType)
		{
			var urlActionPos = contentType.IndexOf("action=\"");
			if (urlActionPos != -1)
			{
				var startIndex = urlActionPos + "action=\"".Length;
				var urlAction = contentType.Substring(
					startIndex,
					contentType.IndexOf('"', startIndex) - startIndex);

				var parts = urlAction.Split('/');
				var operationName = parts.Last();
				return operationName;
			}

			return null;
		}

		public string GetSoapContentType(HttpContext context)
		{
			var requestOperationName = GetAction(context);
			return requestOperationName != null
					? context.Request.ContentType.Replace(requestOperationName, requestOperationName + "Response")
					: ContentType.Soap12;
		}
	}
}