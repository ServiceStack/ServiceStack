using System;
using System.IO;
using System.Net;
using ServiceStack.Host;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests.Support
{
    public class DirectServiceClient : IServiceClient, IRestClient
    {
        ServiceController ServiceController { get; set; }

        readonly MockHttpRequest httpReq = new MockHttpRequest();
        readonly MockHttpResponse httpRes = new MockHttpResponse();

        public DirectServiceClient(ServiceController serviceController)
        {
            this.ServiceController = serviceController;
        }

        public void SendOneWay(object requestDto)
        {
            ServiceController.Execute(requestDto);
        }

        public void SendOneWay(string relativeOrAbsoluteUrl, object request)
        {
            ServiceController.Execute(request);
        }

        private bool ApplyRequestFilters<TResponse>(object request)
        {
            if (HostContext.ApplyRequestFilters(httpReq, httpRes, request))
            {
                ThrowIfError<TResponse>(httpRes);
                return true;
            }
            return false;
        }

        private void ThrowIfError<TResponse>(MockHttpResponse httpRes)
        {
            if (httpRes.StatusCode >= 400)
            {
                var webEx = new WebServiceException("WebServiceException, StatusCode: " + httpRes.StatusCode)
                {
                    StatusCode = httpRes.StatusCode,
                    StatusDescription = httpRes.StatusDescription,
                };

                try
                {
                    var deserializer = HostContext.ContentTypes.GetStreamDeserializer(httpReq.ResponseContentType);
                    webEx.ResponseDto = deserializer(typeof(TResponse), new MemoryStream(httpRes.ReadAsBytes()));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                throw webEx;
            }
        }

        private bool ApplyResponseFilters<TResponse>(object response)
        {
            if (HostContext.ApplyResponseFilters(httpReq, httpRes, response))
            {
                ThrowIfError<TResponse>(httpRes);
                return true;
            }
            return false;
        }

        public TResponse Send<TResponse>(object request)
        {
            httpReq.HttpMethod = HttpMethods.Post;

            if (ApplyRequestFilters<TResponse>(request)) return default(TResponse);

            var response = ServiceController.Execute(request,
                new HttpRequestContext(httpReq, httpRes, request, RequestAttributes.HttpPost));

            if (ApplyResponseFilters<TResponse>(response)) return (TResponse)response;

            return (TResponse)response;
        }

        public TResponse Send<TResponse>(IReturn<TResponse> request)
        {
            throw new NotImplementedException();
        }

        public void Send(IReturnVoid request)
        {
            throw new NotImplementedException();
        }

        public void Get(object request)
        {
            throw new NotImplementedException();
        }

        public TResponse Get<TResponse>(IReturn<TResponse> requestDto)
        {
            throw new NotImplementedException();
        }

        public TResponse Get<TResponse>(object requestDto)
        {
            throw new NotImplementedException();
        }

        public void Get(IReturnVoid request)
        {
            throw new NotImplementedException();
        }

        public TResponse Get<TResponse>(string relativeOrAbsoluteUrl)
        {
            httpReq.HttpMethod = HttpMethods.Get;

            var requestTypeName = typeof(TResponse).Namespace + "." + relativeOrAbsoluteUrl;
            var requestType = typeof(TResponse).Assembly.GetType(requestTypeName);
            if (requestType == null)
                throw new ArgumentException("Type not found: " + requestTypeName);

            var request = requestType.CreateInstance();

            if (ApplyRequestFilters<TResponse>(request)) return default(TResponse);

            var response = ServiceController.Execute(request,
                new HttpRequestContext(httpReq, httpRes, request, RequestAttributes.HttpGet));

            if (ApplyResponseFilters<TResponse>(response)) return (TResponse)response;

            return (TResponse)response;
        }

        public void Delete(object requestDto)
        {
            throw new NotImplementedException();
        }

        public TResponse Delete<TResponse>(IReturn<TResponse> request)
        {
            throw new NotImplementedException();
        }

        public TResponse Delete<TResponse>(object request)
        {
            throw new NotImplementedException();
        }

        public void Delete(IReturnVoid requestDto)
        {
            throw new NotImplementedException();
        }

        public TResponse Delete<TResponse>(string relativeOrAbsoluteUrl)
        {
            throw new NotImplementedException();
        }

        public void Post(object requestDto)
        {
            throw new NotImplementedException();
        }

        public TResponse Post<TResponse>(IReturn<TResponse> requestDto)
        {
            throw new NotImplementedException();
        }

        public TResponse Post<TResponse>(object requestDto)
        {
            throw new NotImplementedException();
        }

        public TResponse Post<TResponse>(string relativeOrAbsoluteUrl, object request)
        {
            throw new NotImplementedException();
        }

        public void Post(IReturnVoid requestDto)
        {
            throw new NotImplementedException();
        }

        public TResponse Post<TResponse>(object request, string relativeOrAbsoluteUrl)
        {
            throw new NotImplementedException();
        }

        public void Put(object requestDto)
        {
            throw new NotImplementedException();
        }

        public TResponse Put<TResponse>(IReturn<TResponse> requestDto)
        {
            throw new NotImplementedException();
        }

        public TResponse Put<TResponse>(object requestDto)
        {
            throw new NotImplementedException();
        }

        public TResponse Put<TResponse>(string relativeOrAbsoluteUrl, object requestDto)
        {
            throw new NotImplementedException();
        }

        public void Put(IReturnVoid requestDto)
        {
            throw new NotImplementedException();
        }

        public TResponse Put<TResponse>(object requestDto, string relativeOrAbsoluteUrl)
        {
            throw new NotImplementedException();
        }

        public void Patch(object requestDto)
        {
            throw new NotImplementedException();
        }

        public TResponse Patch<TResponse>(IReturn<TResponse> requestDto)
        {
            throw new NotImplementedException();
        }

        public TResponse Patch<TResponse>(object requestDto)
        {
            throw new NotImplementedException();
        }

        public TResponse Patch<TResponse>(string relativeOrAbsoluteUrl, object requestDto)
        {
            throw new NotImplementedException();
        }

        public void Patch(IReturnVoid requestDto)
        {
            throw new NotImplementedException();
        }

        public TResponse Patch<TResponse>(object requestDto, string relativeOrAbsoluteUrl)
        {
            throw new NotImplementedException();
        }

        public TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, FileInfo fileToUpload, string mimeType)
        {
            throw new NotImplementedException();
        }

        public void CustomMethod(string httpVerb, IReturnVoid requestDto)
        {
            throw new NotImplementedException();
        }

        public void CustomMethod(string httpVerb, object requestDto)
        {
            throw new NotImplementedException();
        }

        public TResponse CustomMethod<TResponse>(string httpVerb, IReturn<TResponse> requestDto)
        {
            throw new NotImplementedException();
        }

        public TResponse CustomMethod<TResponse>(string httpVerb, object requestDto)
        {
            throw new NotImplementedException();
        }

        public HttpWebResponse Head(IReturn requestDto)
        {
            throw new NotImplementedException();
        }

        public HttpWebResponse Head(object requestDto)
        {
            throw new NotImplementedException();
        }

        public HttpWebResponse Head(string relativeOrAbsoluteUrl)
        {
            throw new NotImplementedException();
        }

        public TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileInfo, string mimeType)
        {
            throw new NotImplementedException();
        }

        public void SendAsync<TResponse>(object requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
        {
            var response = default(TResponse);
            try
            {
                try
                {
                    if (ApplyRequestFilters<TResponse>(requestDto))
                    {
                        onSuccess(default(TResponse));
                        return;
                    }
                }
                catch (Exception ex)
                {
                    onError(default(TResponse), ex);
                    return;
                }

                response = this.Send<TResponse>(requestDto);

                try
                {
                    if (ApplyResponseFilters<TResponse>(requestDto))
                    {
                        onSuccess(response);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    onError(response, ex);
                    return;
                }

                onSuccess(response);
            }
            catch (Exception ex)
            {
                if (onError != null)
                {
                    onError(response, ex);
                    return;
                }
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        public void SetCredentials(string userName, string password)
        {
            throw new NotImplementedException();
        }

        public void GetAsync<TResponse>(IReturn<TResponse> requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
        {
            throw new NotImplementedException();
        }

        public void GetAsync<TResponse>(object requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
        {
            throw new NotImplementedException();
        }

        public void GetAsync<TResponse>(string relativeOrAbsoluteUrl, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
        {
            throw new NotImplementedException();
        }

        public void DeleteAsync<TResponse>(object requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
        {
            throw new NotImplementedException();
        }

        public void DeleteAsync<TResponse>(string relativeOrAbsoluteUrl, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
        {
            throw new NotImplementedException();
        }

        public void DeleteAsync<TResponse>(IReturn<TResponse> requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
        {
            throw new NotImplementedException();
        }

        public void PostAsync<TResponse>(IReturn<TResponse> requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
        {
            throw new NotImplementedException();
        }

        public void PostAsync<TResponse>(object requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
        {
            throw new NotImplementedException();
        }

        public void PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
        {
            throw new NotImplementedException();
        }

        public void PutAsync<TResponse>(IReturn<TResponse> requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
        {
            throw new NotImplementedException();
        }

        public void PutAsync<TResponse>(object requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
        {
            throw new NotImplementedException();
        }

        public void PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
        {
            throw new NotImplementedException();
        }

        public void CustomMethodAsync<TResponse>(string httpVerb, IReturn<TResponse> requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
        {
            throw new NotImplementedException();
        }

        public void CustomMethodAsync<TResponse>(string httpVerb, object requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
        {
            throw new NotImplementedException();
        }

        public void CancelAsync()
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }
        public TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, FileInfo fileToUpload, object request)
        {
            throw new NotImplementedException();
        }

        public TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, object request)
        {
            throw new NotImplementedException();
        }
    }
}