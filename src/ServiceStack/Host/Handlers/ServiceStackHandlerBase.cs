//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.MiniProfiler;
using ServiceStack.Serialization;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public interface IRequestHttpHandler
    {
        string RequestName { get; }

        Task<object> CreateRequestAsync(IRequest req, string operationName);

        Task<object> GetResponseAsync(IRequest httpReq, object request);

        Task HandleResponse(IRequest httpReq, IResponse httpRes, object response);
    }

    public abstract class ServiceStackHandlerBase : HttpAsyncTaskHandler
    {
        internal static readonly Dictionary<byte[], byte[]> NetworkInterfaceIpv4Addresses = new Dictionary<byte[], byte[]>();
        internal static readonly byte[][] NetworkInterfaceIpv6Addresses = TypeConstants.EmptyByteArrayArray;
        internal readonly ServiceStackHost appHost = HostContext.AppHost;

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

        public override bool IsReusable => false;

        public void UpdateResponseContentType(IRequest httpReq, object response)
        {
            var httpResult = response as IHttpResult;
            if (httpResult?.ContentType != null)
            {
                httpReq.ResponseContentType = httpResult.ContentType;
            }
        }

        public virtual Task<object> GetResponseAsync(IRequest httpReq, object request)
        {
            using (Profiler.Current.Step("Execute " + GetType().Name + " Service"))
            {
                return appHost.ServiceController.ExecuteAsync(request, httpReq);
            }
        }

        public async Task HandleResponse(IRequest httpReq, IResponse httpRes, object response)
        {
            if (response is Task taskResponse)
            {
                if (taskResponse.Status == TaskStatus.Created)
                {
                    taskResponse.Start();
                }

                await taskResponse;
                var taskResult = taskResponse.GetResult();

                if (taskResult is Task[] taskResults)
                {
                    var batchResponse = await HandleAsyncBatchResponse(taskResults);
                    await HandleResponseNext(httpReq, httpRes, batchResponse);
                    return;
                }

                if (taskResult is Task subTask)
                {
                    await subTask;
                    taskResult = subTask.GetResult();
                }

                await HandleResponseNext(httpReq, httpRes, taskResult);
            }
            else
            {
                if (response is Task[] taskResults)
                {
                    var batchResponse = await HandleAsyncBatchResponse(taskResults);
                    await HandleResponseNext(httpReq, httpRes, batchResponse);
                    return;
                }

                await HandleResponseNext(httpReq, httpRes, response);
            }
        }

        private async Task HandleResponseNext(IRequest httpReq, IResponse httpRes, object response)
        {
            var callback = httpReq.GetJsonpCallback();
            var doJsonp = HostContext.Config.AllowJsonpRequests && !string.IsNullOrEmpty(callback);

            UpdateResponseContentType(httpReq, response);
            response = await appHost.ApplyResponseConvertersAsync(httpReq, response);

            await appHost.ApplyResponseFiltersAsync(httpReq, httpRes, response);
            if (httpRes.IsClosed)
                return;

            if (httpReq.ResponseContentType.Contains("jsv") &&
                !string.IsNullOrEmpty(httpReq.QueryString[Keywords.Debug]))
            {
                await WriteDebugResponse(httpRes, response);
                return;
            }

            if (doJsonp && !(response is CompressedResult))
            {
                await httpRes.WriteToResponse(httpReq, response, (callback + "(").ToUtf8Bytes(), ")".ToUtf8Bytes());
                return;
            }

            await httpRes.WriteToResponse(httpReq, response);
        }

        private static async Task<object[]> HandleAsyncBatchResponse(Task[] taskResults)
        {
            if (taskResults.Length == 0)
                return TypeConstants.EmptyObjectArray;

            await Task.WhenAll(taskResults);

            var firstResponse = taskResults[0].GetResult();
            var batchedResponses = firstResponse != null
                ? (object[]) Array.CreateInstance(firstResponse.GetType(), taskResults.Length)
                : new object[taskResults.Length];

            batchedResponses[0] = firstResponse;
            for (var i = 1; i < taskResults.Length; i++)
            {
                batchedResponses[i] = taskResults[i].GetResult();
            }

            return batchedResponses;
        }

        public static object DeserializeHttpRequest(Type operationType, IRequest httpReq, string contentType)
        {
            var httpMethod = httpReq.Verb;
            var queryString = httpReq.QueryString;
            var hasRequestBody = httpReq.ContentType != null && httpReq.ContentLength > 0;

            if (!hasRequestBody
                && (httpMethod == HttpMethods.Get || httpMethod == HttpMethods.Delete || httpMethod == HttpMethods.Options))
            {
                return KeyValueDataContractDeserializer.Instance.Parse(queryString, operationType);
            }

            var isFormData = httpReq.HasAnyOfContentTypes(MimeTypes.FormUrlEncoded, MimeTypes.MultiPartFormData);
            if (isFormData)
            {
                return KeyValueDataContractDeserializer.Instance.Parse(httpReq.FormData, operationType);
            }

            var request = CreateContentTypeRequest(httpReq, operationType, contentType);
            return request;
        }

        protected static object CreateContentTypeRequest(IRequest httpReq, Type requestType, string contentType)
        {
            try
            {
                if (!string.IsNullOrEmpty(contentType))
                {
                    //.NET Core HttpClient Zip Content-Length omission is reported as 0
                    var hasContentBody = httpReq.ContentLength > 0
                        || (HttpUtils.HasRequestBody(httpReq.Verb) && httpReq.GetContentEncoding() != null);

                    if (hasContentBody)
                    {
                        var deserializer = HostContext.ContentTypes.GetStreamDeserializer(contentType);
                        if (deserializer != null)
                        {
                            return deserializer(requestType, httpReq.InputStream);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = $"Could not deserialize '{contentType}' request using {requestType}'\nError: {ex}";
                throw new SerializationException(msg, ex);
            }
            return requestType.CreateInstance(); //Return an empty DTO, even for empty request bodies
        }

        protected static object GetCustomRequestFromBinder(IRequest httpReq, Type requestType)
        {
            Func<IRequest, object> requestFactoryFn;
            HostContext.ServiceController.RequestTypeFactoryMap.TryGetValue(
                requestType, out requestFactoryFn);

            return requestFactoryFn?.Invoke(httpReq);
        }

        public static Type GetOperationType(string operationName)
        {
            return HostContext.Metadata.GetOperationType(operationName);
        }

        protected static object ExecuteService(object request, IRequest httpReq)
        {
            return HostContext.ExecuteService(request, httpReq);
        }

        protected static void AssertOperationExists(string operationName, Type type)
        {
            if (type == null)
                throw new NotImplementedException($"The operation '{operationName}' does not exist for this service");
        }

        protected bool AssertAccess(IHttpRequest httpReq, IHttpResponse httpRes, Feature feature, string operationName)
        {
            if (operationName == null)
                throw new ArgumentNullException(nameof(operationName));

            if (HostContext.Config.EnableFeatures != Feature.All)
            {
                if (!HostContext.HasFeature(feature))
                {
                    appHost.HandleErrorResponse(httpReq, httpRes, HttpStatusCode.Forbidden, "Feature Not Available");
                    return false;
                }
            }

            var format = feature.ToFormat();
            if (!HostContext.Metadata.CanAccess(httpReq, format, operationName))
            {
                appHost.HandleErrorResponse(httpReq, httpRes, HttpStatusCode.Forbidden, "Service Not Available");
                return false;
            }
            return true;
        }

        private static Task WriteDebugRequest(IRequest requestContext, object dto, Stream stream)
        {
            var bytes = Encoding.UTF8.GetBytes(dto.SerializeAndFormat());
            return stream.WriteAsync(bytes, 0, bytes.Length);
        }

        public Task WriteDebugResponse(IResponse httpRes, object response)
        {
            return httpRes.WriteToResponse(response, WriteDebugRequest,
                new BasicRequest { ContentType = MimeTypes.PlainText });
        }
    }
}
