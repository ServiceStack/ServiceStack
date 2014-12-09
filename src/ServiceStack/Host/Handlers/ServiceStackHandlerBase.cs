//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Serialization;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public abstract class ServiceStackHandlerBase : HttpAsyncTaskHandler
    {
        internal static readonly ILog Log = LogManager.GetLogger(typeof(ServiceStackHandlerBase));
        internal static readonly Dictionary<byte[], byte[]> NetworkInterfaceIpv4Addresses = new Dictionary<byte[], byte[]>();
        internal static readonly byte[][] NetworkInterfaceIpv6Addresses = new byte[0][];

        static ServiceStackHandlerBase()
        {
            try
            {
                IPAddressExtensions.GetAllNetworkInterfaceIpv4Addresses().ForEach((x, y) => NetworkInterfaceIpv4Addresses[x.GetAddressBytes()] = y.GetAddressBytes());

                NetworkInterfaceIpv6Addresses = IPAddressExtensions.GetAllNetworkInterfaceIpv6Addresses().ConvertAll(x => x.GetAddressBytes()).ToArray();
            }
            catch (Exception ex)
            {
                Log.Warn("Failed to retrieve IP Addresses, some security restriction features may not work: " + ex.Message, ex);
            }
        }

        public RequestAttributes HandlerAttributes { get; set; }

        public override bool IsReusable
        {
            get { return false; }
        }

        public abstract object CreateRequest(IRequest request, string operationName);
        public abstract object GetResponse(IRequest request, object requestDto);

        public Task HandleResponse(object response, Func<object, Task> callback, Func<Exception, Task> errorCallback)
        {
            try
            {
                var taskResponse = response as Task;
                if (taskResponse != null)
                {
                    if (taskResponse.Status == TaskStatus.Created)
                    {
                        taskResponse.Start();
                    }

                    return taskResponse
                        .Continue(task =>
                        {
                            if (task.IsFaulted)
                                return errorCallback(task.Exception.UnwrapIfSingleException());

                            if (task.IsCanceled)
                                return errorCallback(new OperationCanceledException("The async Task operation was cancelled"));

                            if (task.IsCompleted)
                            {
                                var taskResult = task.GetResult();
                                return callback(taskResult);
                            }

                            return errorCallback(new InvalidOperationException("Unknown Task state"));
                        });
                }

                return callback(response);
            }
            catch (Exception ex)
            {
                return errorCallback(ex);
            }
        }

        public static object DeserializeHttpRequest(Type operationType, IRequest httpReq, string contentType)
        {
            var httpMethod = httpReq.Verb;
            var queryString = httpReq.QueryString;
            var hasRequestBody = httpReq.ContentType != null && httpReq.ContentLength > 0;

            if (!hasRequestBody
                && (httpMethod == HttpMethods.Get || httpMethod == HttpMethods.Delete || httpMethod == HttpMethods.Options))
            {
                try
                {
                    return KeyValueDataContractDeserializer.Instance.Parse(queryString, operationType);
                }
                catch (Exception ex)
                {
                    var msg = "Could not deserialize '{0}' request using KeyValueDataContractDeserializer: '{1}'.\nError: '{2}'"
                        .Fmt(operationType, queryString, ex);
                    throw new SerializationException(msg);
                }
            }

            var isFormData = httpReq.HasAnyOfContentTypes(MimeTypes.FormUrlEncoded, MimeTypes.MultiPartFormData);
            if (isFormData)
            {
                try
                {
                    return KeyValueDataContractDeserializer.Instance.Parse(httpReq.FormData, operationType);
                }
                catch (Exception ex)
                {
                    throw new SerializationException("Error deserializing FormData: " + httpReq.FormData, ex);
                }
            }

            var request = CreateContentTypeRequest(httpReq, operationType, contentType);
            return request;
        }

        protected static object CreateContentTypeRequest(IRequest httpReq, Type requestType, string contentType)
        {
            try
            {
                if (!string.IsNullOrEmpty(contentType) && httpReq.ContentLength > 0)
                {
                    var deserializer = HostContext.ContentTypes.GetStreamDeserializer(contentType);
                    if (deserializer != null)
                    {
                        return deserializer(requestType, httpReq.InputStream);
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = "Could not deserialize '{0}' request using {1}'\nError: {2}"
                    .Fmt(contentType, requestType, ex);
                throw new SerializationException(msg);
            }
            return requestType.CreateInstance(); //Return an empty DTO, even for empty request bodies
        }

        protected static object GetCustomRequestFromBinder(IRequest httpReq, Type requestType)
        {
            Func<IRequest, object> requestFactoryFn;
            HostContext.ServiceController.RequestTypeFactoryMap.TryGetValue(
                requestType, out requestFactoryFn);

            return requestFactoryFn != null ? requestFactoryFn(httpReq) : null;
        }

        public static Type GetOperationType(string operationName)
        {
            return HostContext.Metadata.GetOperationType(operationName);
        }

        protected static object ExecuteService(object request, IRequest httpReq)
        {
            return HostContext.ExecuteService(request, httpReq);
        }

        public RequestAttributes GetRequestAttributes(System.ServiceModel.OperationContext operationContext)
        {
            if (!HostContext.Config.EnableAccessRestrictions) return default(RequestAttributes);

            var portRestrictions = default(RequestAttributes);
            var ipAddress = GetIpAddress(operationContext);

            portRestrictions |= HttpRequestExtensions.GetAttributes(ipAddress);

            //TODO: work out if the request was over a secure channel			
            //portRestrictions |= request.IsSecureConnection ? PortRestriction.Secure : PortRestriction.InSecure;

            return portRestrictions;
        }

        public static IPAddress GetIpAddress(System.ServiceModel.OperationContext context)
        {
#if !MONO
            var prop = context.IncomingMessageProperties;
            if (context.IncomingMessageProperties.ContainsKey(System.ServiceModel.Channels.RemoteEndpointMessageProperty.Name))
            {
                var endpoint = prop[System.ServiceModel.Channels.RemoteEndpointMessageProperty.Name]
                    as System.ServiceModel.Channels.RemoteEndpointMessageProperty;
                if (endpoint != null)
                {
                    return IPAddress.Parse(endpoint.Address);
                }
            }
#endif
            return null;
        }

        protected static void AssertOperationExists(string operationName, Type type)
        {
            if (type == null)
            {
                throw new NotImplementedException(
                    string.Format("The operation '{0}' does not exist for this service", operationName));
            }
        }

        protected bool AssertAccess(IHttpRequest httpReq, IHttpResponse httpRes, Feature feature, string operationName)
        {
            if (operationName == null)
                throw new ArgumentNullException("operationName");

            if (HostContext.Config.EnableFeatures != Feature.All)
            {
                if (!HostContext.HasFeature(feature))
                {
                    HostContext.AppHost.HandleErrorResponse(httpReq, httpRes, HttpStatusCode.Forbidden, "Feature Not Available");
                    return false;
                }
            }

            var format = feature.ToFormat();
            if (!HostContext.Metadata.CanAccess(httpReq, format, operationName))
            {
                HostContext.AppHost.HandleErrorResponse(httpReq, httpRes, HttpStatusCode.Forbidden, "Service Not Available");
                return false;
            }
            return true;
        }

        private static void WriteDebugRequest(IRequest requestContext, object dto, IResponse httpRes)
        {
            var bytes = Encoding.UTF8.GetBytes(dto.SerializeAndFormat());
            httpRes.OutputStream.Write(bytes, 0, bytes.Length);
        }

        public Task WriteDebugResponse(IResponse httpRes, object response)
        {
            return httpRes.WriteToResponse(response, WriteDebugRequest,
                new BasicRequest { ContentType = MimeTypes.PlainText });
        }
    }
}
