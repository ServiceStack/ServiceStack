using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Web;
using System.Xml;
using ServiceStack.Serialization;
using ServiceStack.Server;
using ServiceStack.Clients;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Utils;
using HttpRequestWrapper = ServiceStack.WebHost.Endpoints.Wrappers.HttpRequestWrapper;
using HttpResponseWrapper = ServiceStack.WebHost.Endpoints.Wrappers.HttpResponseWrapper;

namespace ServiceStack.WebHost.Endpoints.Support
{
    public abstract class SoapHandler : EndpointHandlerBase, IOneWay, ISyncReply
    {
        public SoapHandler(EndpointAttributes soapType)
        {
            this.HandlerAttributes = soapType;
        }

        public void SendOneWay(Message requestMsg)
        {
            SendOneWay(requestMsg, null, null);
        }

        protected void SendOneWay(Message requestMsg, IHttpRequest httpRequest, IHttpResponse httpResponse)
        {
            var endpointAttributes = EndpointAttributes.OneWay | this.HandlerAttributes;

            ExecuteMessage(requestMsg, endpointAttributes, httpRequest, httpResponse);
        }

        protected abstract Message GetRequestMessageFromStream(Stream requestStream);

        public Message Send(Message requestMsg)
        {
            var endpointAttributes = EndpointAttributes.Reply | this.HandlerAttributes;

            return ExecuteMessage(requestMsg, endpointAttributes, null, null);
        }
        
        protected Message Send(Message requestMsg, IHttpRequest httpRequest, IHttpResponse httpResponse)
        {
            var endpointAttributes = EndpointAttributes.Reply | this.HandlerAttributes;

            return ExecuteMessage(requestMsg, endpointAttributes, httpRequest, httpResponse);
        }

        public Message EmptyResponse(Message requestMsg, Type requestType)
        {
            var responseType = AssemblyUtils.FindType(requestType.FullName + "Response");
            var response = (responseType ?? typeof(object)).CreateInstance();

            return requestMsg.Headers.Action == null
                ? Message.CreateMessage(requestMsg.Version, null, response)
                : Message.CreateMessage(requestMsg.Version, requestType.Name + "Response", response);
        }

        protected Message ExecuteMessage(Message message, EndpointAttributes endpointAttributes, IHttpRequest httpRequest, IHttpResponse httpResponse)
        {
            var soapFeature = endpointAttributes.ToSoapFeature();
            EndpointHost.Config.AssertFeatures(soapFeature);

            var httpReq = HttpContext.Current != null && httpRequest == null
                    ? new HttpRequestWrapper(HttpContext.Current.Request)
                    : httpRequest;
            var httpRes = HttpContext.Current != null && httpResponse == null
                ? new HttpResponseWrapper(HttpContext.Current.Response)
                : httpResponse;

            if (httpReq == null)
                throw new ArgumentNullException("httpRequest");

            if (httpRes == null)
                throw new ArgumentNullException("httpResponse");

            if (EndpointHost.ApplyPreRequestFilters(httpReq, httpRes))
                return PrepareEmptyResponse(message, httpReq);

            var requestMsg = message ?? GetRequestMessageFromStream(httpReq.InputStream);
            string requestXml = GetRequestXml(requestMsg);
            var requestType = GetRequestType(requestMsg, requestXml);
            if (!EndpointHost.Metadata.CanAccess(endpointAttributes, soapFeature.ToFormat(), requestType.Name))
                throw EndpointHost.Config.UnauthorizedAccess(endpointAttributes);

            try
            {
                var useXmlSerializerRequest = requestType.HasAttribute<XmlSerializerFormatAttribute>();

                var request = useXmlSerializerRequest
                                  ? XmlSerializableDeserializer.Instance.Parse(requestXml, requestType)
                                  : DataContractDeserializer.Instance.Parse(requestXml, requestType);
                
                var requiresSoapMessage = request as IRequiresSoapMessage;
                if (requiresSoapMessage != null)
                {
                    requiresSoapMessage.Message = requestMsg;
                }

                httpReq.OperationName = requestType.Name;
                httpReq.SetItem("SoapMessage", requestMsg);

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

                var useXmlSerializerResponse = response.GetType().HasAttribute<XmlSerializerFormatAttribute>();
                
                if (useXmlSerializerResponse)
                    return requestMsg.Headers.Action == null
                        ? Message.CreateMessage(requestMsg.Version, null, response, new XmlSerializerWrapper(response.GetType()))
                        : Message.CreateMessage(requestMsg.Version, requestType.Name + "Response", response, new XmlSerializerWrapper(response.GetType()));
                
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

        private Message PrepareEmptyResponse(Message message, IHttpRequest httpRequest)
        {
            var requestMessage = message ?? GetRequestMessageFromStream(httpRequest.InputStream);
            string requestXml = GetRequestXml(requestMessage);
            var requestType = GetRequestType(requestMessage, requestXml);
            return EmptyResponse(requestMessage, requestType);
        }

        private static string GetRequestXml(Message requestMsg)
        {
            string requestXml;
            using (var reader = requestMsg.GetReaderAtBodyContents())
            {
                requestXml = reader.ReadOuterXml();
            }
            return requestXml;
        }

        protected static Message GetSoap12RequestMessage(Stream inputStream)
        {
            return GetRequestMessage(inputStream, MessageVersion.Soap12WSAddressingAugust2004);
        }

        protected static Message GetSoap11RequestMessage(Stream inputStream)
        {
            return GetRequestMessage(inputStream, MessageVersion.Soap11WSAddressingAugust2004);
        }

        protected static Message GetRequestMessage(Stream inputStream, MessageVersion msgVersion)
        {
            using (var sr = new StreamReader(inputStream))
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

            var operationType = EndpointHost.Metadata.GetOperationType(action);
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
            return context == null ? null : GetAction(context.Request.ContentType);
        }

        private static string GetAction(string contentType)
        {
            if (contentType != null)
            {
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

        public string GetSoapContentType(string contentType)
        {
            var requestOperationName = GetAction(contentType);
            return requestOperationName != null
                    ? contentType.Replace(requestOperationName, requestOperationName + "Response")
                    : (this.HandlerAttributes == EndpointAttributes.Soap11 ? MimeTypes.Soap11 : MimeTypes.Soap12);
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
