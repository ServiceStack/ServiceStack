using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using System.Web;
using System.Xml;
using ServiceStack.Common.Web;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Utils;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public class SoapHandler : EndpointHandlerBase, IOneWay, ISyncReply
	{
		public SoapHandler(EndpointAttributes soapType)
		{
			this.HandlerAttributes = soapType;
		}

		public void SendOneWay(Message requestMsg)
		{
			var endpointAttributes = EndpointAttributes.AsyncOneWay | this.HandlerAttributes;

			ExecuteMessage(requestMsg, endpointAttributes);
		}

		public Message Send(Message requestMsg)
		{
			var endpointAttributes = EndpointAttributes.SyncReply | this.HandlerAttributes;

			return ExecuteMessage(requestMsg, endpointAttributes);
		}

        public Message EmptyResponse(Message requestMsg, Type requestType)
        {
            var responseType = AssemblyUtils.FindType(requestType.FullName + "Response");
            var response = (responseType ?? typeof(object)).CreateInstance();

            return requestMsg.Headers.Action == null
                ? Message.CreateMessage(requestMsg.Version, null, response)
                : Message.CreateMessage(requestMsg.Version, requestType.Name + "Response", response);
        }

		protected Message ExecuteMessage(Message requestMsg, EndpointAttributes endpointAttributes)
		{
			if ((EndpointAttributes.Soap11 & this.HandlerAttributes) == EndpointAttributes.Soap11)
				EndpointHost.Config.AssertFeatures(Feature.Soap11);
			else if ((EndpointAttributes.Soap12 & this.HandlerAttributes) == EndpointAttributes.Soap12)
				EndpointHost.Config.AssertFeatures(Feature.Soap12);

			string requestXml;
			using (var reader = requestMsg.GetReaderAtBodyContents())
			{
				requestXml = reader.ReadOuterXml();
			}

			var requestType = GetRequestType(requestMsg, requestXml);
		    try
			{
				var request = DataContractDeserializer.Instance.Parse(requestXml, requestType);

			    var httpReq = HttpContext.Current != null 
                    ? new HttpRequestWrapper(requestType.Name, HttpContext.Current.Request)
                    : null;
				var httpRes = HttpContext.Current != null 
                    ? new HttpResponseWrapper(HttpContext.Current.Response)
                    : null;

                if (EndpointHost.ApplyPreRequestFilters(httpReq, httpRes))
                    return EmptyResponse(requestMsg, requestType);

				var hasRequestFilters = EndpointHost.RequestFilters.Count > 0 
                    || FilterAttributeCache.GetRequestFilterAttributes(request.GetType()).Any();

				if (hasRequestFilters && EndpointHost.ApplyRequestFilters(httpReq, httpRes, request)) 
                    return EmptyResponse(requestMsg, requestType);

				var response = ExecuteService(request, endpointAttributes, httpReq, httpRes);

				var hasResponseFilters = EndpointHost.ResponseFilters.Count > 0
				   || FilterAttributeCache.GetResponseFilterAttributes(response.GetType()).Any();

				if (hasResponseFilters && EndpointHost.ApplyResponseFilters(httpReq, httpRes, response))
                    return EmptyResponse(requestMsg, requestType);

				var httpResult = response as IHttpResult;
				if (httpResult != null)
					response = httpResult.Response;

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

		protected static Message GetSoap12RequestMessage(HttpContext context)
		{
			return GetRequestMessage(context, MessageVersion.Soap12WSAddressingAugust2004);
		}

		protected static Message GetSoap11RequestMessage(HttpContext context)
		{
			return GetRequestMessage(context, MessageVersion.Soap11WSAddressingAugust2004);
		}

		protected static Message GetRequestMessage(HttpContext context, MessageVersion msgVersion)
		{
			using (var sr = new StreamReader(context.Request.InputStream))
			{
				var requestXml = sr.ReadToEnd();

				var doc = new XmlDocument();
				doc.LoadXml(requestXml);

				var msg = Message.CreateMessage(new XmlNodeReader(doc), int.MaxValue,
					msgVersion);

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

			return xml.StartsWith("<") 
				? xml.Substring(1, xml.IndexOf(" ") - 1).SplitOnFirst(':').Last()
				: null;
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
					: (this.HandlerAttributes == EndpointAttributes.Soap11 ? ContentType.Soap11 : ContentType.Soap12);
		}

		public override object CreateRequest(IHttpRequest request, string operationName)
		{
			throw new NotImplementedException();
		}

		public override object GetResponse(IHttpRequest httpReq, IHttpResponse httpRes, object request)
		{
			throw new NotImplementedException();
		}
	}
}