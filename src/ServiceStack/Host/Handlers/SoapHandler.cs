using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using ServiceStack.Host.AspNet;
using ServiceStack.Serialization;
using ServiceStack.Support.WebHost;
using ServiceStack.Text;
using ServiceStack.Text.FastMember;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public abstract class SoapHandler : ServiceStackHandlerBase, IOneWay, ISyncReply
    {
        public SoapHandler(RequestAttributes soapType)
        {
            this.HandlerAttributes = soapType;
        }

        public void SendOneWay(Message requestMsg)
        {
            SendOneWay(requestMsg, null, null);
        }

        protected void SendOneWay(Message requestMsg, IRequest httpRequest, IResponse httpResponse)
        {
            var endpointAttributes = RequestAttributes.OneWay | this.HandlerAttributes;

            ExecuteMessage(requestMsg, endpointAttributes, httpRequest, httpResponse);
        }

        protected abstract Message GetRequestMessageFromStream(Stream requestStream);

        public Message Send(Message requestMsg)
        {
            var endpointAttributes = RequestAttributes.Reply | this.HandlerAttributes;

            return ExecuteMessage(requestMsg, endpointAttributes, null, null);
        }

        protected Message Send(Message requestMsg, IRequest httpRequest, IResponse httpResponse)
        {
            var endpointAttributes = RequestAttributes.Reply | this.HandlerAttributes;

            return ExecuteMessage(requestMsg, endpointAttributes, httpRequest, httpResponse);
        }

        public Message EmptyResponse(Message requestMsg, Type requestType)
        {
            var responseType = AssemblyUtils.FindType(requestType.FullName + "Response");
            var response = (responseType ?? WebRequestUtils.GetErrorResponseDtoType(requestType)).CreateInstance();

            return requestMsg.Headers.Action == null
                ? Message.CreateMessage(requestMsg.Version, null, response)
                : Message.CreateMessage(requestMsg.Version, requestType.GetOperationName() + "Response", response);
        }

        protected Message ExecuteMessage(Message message, RequestAttributes requestAttributes, IRequest httpReq, IResponse httpRes)
        {
            var soapFeature = requestAttributes.ToSoapFeature();
            HostContext.AppHost.AssertFeatures(soapFeature);

            if (httpReq == null)
                httpReq = HostContext.GetCurrentRequest();

            if (httpRes == null && httpReq != null)
                httpRes = httpReq.Response;

            if (httpReq == null)
                throw new ArgumentNullException("httpReq");

            if (httpRes == null)
                throw new ArgumentNullException("httpRes");

            httpReq.UseBufferedStream = true;
            var requestMsg = message ?? GetRequestMessageFromStream(httpReq.InputStream);

            var soapAction = httpReq.GetHeader(HttpHeaders.SOAPAction)
                ?? GetAction(requestMsg);

            if (soapAction != null)
            {
                httpReq.OperationName = soapAction.Trim('"');
            }

            if (HostContext.ApplyPreRequestFilters(httpReq, httpRes))
                return PrepareEmptyResponse(message, httpReq);

            string requestXml = GetRequestXml(requestMsg);
            var requestType = GetRequestType(requestMsg, requestXml);
            httpReq.OperationName = requestType.GetOperationName();
            if (!HostContext.Metadata.CanAccess(requestAttributes, soapFeature.ToFormat(), requestType.GetOperationName()))
                throw HostContext.UnauthorizedAccess(requestAttributes);

            try
            {
                var useXmlSerializerRequest = requestType.HasAttribute<XmlSerializerFormatAttribute>();

                var request = useXmlSerializerRequest
                    ? XmlSerializableSerializer.Instance.DeserializeFromString(requestXml, requestType)
                    : Serialization.DataContractSerializer.Instance.DeserializeFromString(requestXml, requestType);

                httpReq.Dto = request;

                var requiresSoapMessage = request as IRequiresSoapMessage;
                if (requiresSoapMessage != null)
                {
                    requiresSoapMessage.Message = requestMsg;
                }

                httpReq.SetItem("SoapMessage", requestMsg);

                httpRes.ContentType = GetSoapContentType(httpReq.ContentType);

                var hasRequestFilters = HostContext.GlobalRequestFilters.Count > 0
                    || FilterAttributeCache.GetRequestFilterAttributes(request.GetType()).Any();

                if (hasRequestFilters && HostContext.ApplyRequestFilters(httpReq, httpRes, request))
                    return EmptyResponse(requestMsg, requestType);

                httpReq.RequestAttributes |= requestAttributes;
                var response = ExecuteService(request, httpReq);

                var taskResponse = response as Task;
                if (taskResponse != null)
                {
                    taskResponse.Wait();
                    response = TypeAccessor.Create(taskResponse.GetType())[taskResponse, "Result"];
                }

                var hasResponseFilters = HostContext.GlobalResponseFilters.Count > 0
                   || FilterAttributeCache.GetResponseFilterAttributes(response.GetType()).Any();

                if (hasResponseFilters && HostContext.ApplyResponseFilters(httpReq, httpRes, response))
                    return EmptyResponse(requestMsg, requestType);

                var httpResult = response as IHttpResult;
                if (httpResult != null)
                    response = httpResult.Response;

                var noMsgAction = requestMsg.Headers.Action == null;
                var responseMsg = CreateResponseMessage(response, requestMsg.Version, requestType, noMsgAction);

                if (httpResult != null)
                {
                    SetErrorStatusIfAny(httpReq.Response, responseMsg, httpResult.Status);
                }

                return responseMsg;
            }
            catch (Exception ex)
            {
                throw new SerializationException("3) Error trying to deserialize requestType: "
                    + requestType
                    + ", xml body: " + requestXml, ex);
            }
        }

        public static string GetAction(Message message)
        {
            var headers = message.Headers;
            for (var i = 0; i < headers.Count; i++)
            {
                var header = headers[i];
                if (header.Name != "Action") continue;

                var xr = headers.GetReaderAtHeader(i);
                return xr.ReadElementContentAsString();
            }
            return null;
        }

        public static Message CreateResponseMessage(object response, MessageVersion msgVersion, Type requestType, bool noMsgAction)
        {
            var useXmlSerializerResponse = response != null && response.GetType().HasAttribute<XmlSerializerFormatAttribute>();

            if (useXmlSerializerResponse)
            {
                return noMsgAction
                    ? Message.CreateMessage(msgVersion, null, response,
                        new XmlSerializerWrapper(response.GetType()))
                    : Message.CreateMessage(msgVersion, requestType.GetOperationName() + "Response", response,
                        new XmlSerializerWrapper(response.GetType()));
            }

            return noMsgAction
                ? Message.CreateMessage(msgVersion, null, response)
                : Message.CreateMessage(msgVersion, requestType.GetOperationName() + "Response", response);
        }

        private Message PrepareEmptyResponse(Message message, IRequest req)
        {
            //Usually happens
            if (req.Response.IsClosed)
                return null;

            var requestMessage = message ?? GetRequestMessageFromStream(req.InputStream);
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

        public static byte[] SerializeSoap11ToBytes(IRequest req, object response)
        {
            using (var ms = MemoryStreamFactory.GetStream())
            {
                SerializeSoapToStream(req, response, MessageVersion.Soap11WSAddressingAugust2004, ms);
                return ms.ToArray();
            }
        }

        public static byte[] SerializeSoap12ToBytes(IRequest req, object response)
        {
            using (var ms = MemoryStreamFactory.GetStream())
            {
                SerializeSoapToStream(req, response, MessageVersion.Soap12WSAddressingAugust2004, ms);
                return ms.ToArray();
            }
        }

        public static void SerializeSoap11ToStream(IRequest req, object response, Stream stream)
        {
            SerializeSoapToStream(req, response, MessageVersion.Soap11WSAddressingAugust2004, stream);
        }

        public static void SerializeSoap12ToStream(IRequest req, object response, Stream stream)
        {
            SerializeSoapToStream(req, response, MessageVersion.Soap12WSAddressingAugust2004, stream);
        }

        private static void SerializeSoapToStream(IRequest req, object response, MessageVersion defaultMsgVersion, Stream stream)
        {
            var requestMsg = req.GetItem("SoapMessage") as Message;
            var msgVersion = requestMsg != null
                ? requestMsg.Version
                : defaultMsgVersion;

            var noMsgVersion = requestMsg == null || requestMsg.Headers.Action == null;

            var responseMsg = CreateResponseMessage(response, msgVersion, req.Dto.GetType(), noMsgVersion);
            SetErrorStatusIfAny(req.Response, responseMsg, req.Response.StatusCode);

            using (var writer = CreateXmlWriter(stream))
            {
                responseMsg.WriteMessage(writer);
            }
        }

        private static void SetErrorStatusIfAny(IResponse res, Message responseMsg, int statusCode)
        {
            if (statusCode >= 400)
            {
                res.AddHeader(HttpHeaders.XStatus, statusCode.ToString());
                res.StatusCode = 200;
                responseMsg.Headers.Add(
                    MessageHeader.CreateHeader(
                        HttpHeaders.XStatus,
                        HostContext.Config.WsdlServiceNamespace,
                        statusCode.ToString(),
                        false));
            }
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

            var operationType = HostContext.Metadata.GetOperationType(action);
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
                    : (this.HandlerAttributes == RequestAttributes.Soap11 ? MimeTypes.Soap11 : MimeTypes.Soap12);
        }

        public override object CreateRequest(IRequest request, string operationName)
        {
            throw new NotImplementedException();
        }

        public override object GetResponse(IRequest httpReq, object request)
        {
            throw new NotImplementedException();
        }

        public static XmlWriter CreateXmlWriter(Stream stream)
        {
            return XmlWriter.Create(stream, HostContext.Config.XmlWriterSettings);
        }
    }
}
