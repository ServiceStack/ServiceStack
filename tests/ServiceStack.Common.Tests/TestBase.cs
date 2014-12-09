using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Messaging;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Common.Tests
{
    public abstract class TestBase
    {
        protected ServiceStackHost AppHost { get; set; }

        protected bool HasConfigured { get; set; }

        protected TestBase(params Assembly[] serviceAssemblies)
            : this(null, serviceAssemblies) { }

        protected TestBase(string serviceClientBaseUri, params Assembly[] serviceAssemblies)
        {
            if (serviceAssemblies.Length == 0)
                serviceAssemblies = new[] { GetType().Assembly };

            ServiceClientBaseUri = serviceClientBaseUri;
            ServiceAssemblies = serviceAssemblies;

            this.AppHost = new BasicAppHost(serviceAssemblies).Init();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            this.AppHost.Dispose();
        }

        protected abstract void Configure(Funq.Container container);

        protected Funq.Container Container
        {
            get { return HostContext.Container; }
        }

        protected IServiceRoutes Routes
        {
            get { return HostContext.AppHost.Routes; }
        }

        //All integration tests call the Webservices hosted at the following location:
        protected string ServiceClientBaseUri { get; set; }
        protected Assembly[] ServiceAssemblies { get; set; }

        public virtual void OnBeforeTestFixture()
        {
            OnConfigure();
        }

        protected virtual void OnConfigure()
        {
            if (HasConfigured) return;

            HasConfigured = true;
            Configure(Container);
        }

        public virtual void OnBeforeEachTest()
        {
            OnConfigure();
        }

        protected virtual IServiceClient CreateNewServiceClient()
        {
            return new DirectServiceClient(this, AppHost.ServiceController);
        }

        protected virtual IRestClient CreateNewRestClient()
        {
            return new DirectServiceClient(this, AppHost.ServiceController);
        }

        protected virtual IRestClientAsync CreateNewRestClientAsync()
        {
            return new DirectServiceClient(this, AppHost.ServiceController);
        }

        public class DirectServiceClient : IServiceClient, IRestClient
        {
            private readonly TestBase parent;
            ServiceController ServiceManager { get; set; }

            public DirectServiceClient(TestBase parent, ServiceController serviceManager)
            {
                this.parent = parent;
                this.ServiceManager = serviceManager;
            }

            public void SendOneWay(object requestDto)
            {
                ServiceManager.Execute(requestDto);
            }

            public void SendOneWay(string relativeOrAbsoluteUri, object requestDto)
            {
                ServiceManager.Execute(requestDto);
            }

            public void SendAllOneWay(IEnumerable<object> requests)
            {
                throw new NotImplementedException();
            }

            public TResponse Send<TResponse>(object request)
            {
                var message = MessageFactory.Create(request);
                var response = ServiceManager.ExecuteMessage(message);
                var httpResult = response as IHttpResult;
                if (httpResult != null)
                {
                    if (httpResult.StatusCode >= HttpStatusCode.BadRequest)
                    {
                        var webEx = new WebServiceException(httpResult.StatusDescription) {
                            ResponseDto = httpResult.Response,
                            StatusCode = httpResult.Status,
                        };
                        throw webEx;
                    }
                    return (TResponse) httpResult.Response;
                }

                var responseStatus = response.GetResponseStatus();
                var isError = responseStatus != null && responseStatus.ErrorCode != null;
                if (isError)
                {
                    var webEx = new WebServiceException(responseStatus.Message)
                    {
                        ResponseDto = response,
                        StatusCode = responseStatus.Errors != null && responseStatus.Errors.Count > 0
                            ? 400
                            : 500,
                    };
                    throw webEx;
                }

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

            public List<TResponse> SendAll<TResponse>(IEnumerable<IReturn<TResponse>> requests)
            {
                throw new NotImplementedException();
            }

            public HttpWebResponse Get(object request)
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
                return parent.ExecutePath<TResponse>(HttpMethods.Get, new UrlParts(relativeOrAbsoluteUrl), null);
            }

            public IEnumerable<TResponse> GetLazy<TResponse>(IReturn<QueryResponse<TResponse>> queryDto)
            {
                throw new NotImplementedException();
            }

            public HttpWebResponse Delete(object requestDto)
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
                return parent.ExecutePath<TResponse>(HttpMethods.Delete, new UrlParts(relativeOrAbsoluteUrl), null);
            }

            public HttpWebResponse Post(object requestDto)
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
                return parent.ExecutePath<TResponse>(HttpMethods.Post, new UrlParts(relativeOrAbsoluteUrl), request);
            }

            public HttpWebResponse Put(object requestDto)
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
                return parent.ExecutePath<TResponse>(HttpMethods.Put, new UrlParts(relativeOrAbsoluteUrl), requestDto);
            }

            public HttpWebResponse Patch(object requestDto)
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
                return parent.ExecutePath<TResponse>(HttpMethods.Patch, new UrlParts(relativeOrAbsoluteUrl), requestDto);
            }

            public TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, FileInfo fileToUpload, string mimeType)
            {
                throw new NotImplementedException();
            }

            public void CustomMethod(string httpVerb, IReturnVoid requestDto)
            {
                throw new NotImplementedException();
            }

            public HttpWebResponse CustomMethod(string httpVerb, object requestDto)
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

            public TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string mimeType)
            {
                throw new NotImplementedException();
            }

            public TResponse PostFileWithRequest<TResponse>(
                Stream fileToUpload, string fileName, object request, string fieldName = "upload")
            {
                throw new NotImplementedException();
            }

            public Task<TResponse> SendAsync<TResponse>(object requestDto)
            {
                var tcs = new TaskCompletionSource<TResponse>();
                try
                {
                    var response = (TResponse)ServiceManager.Execute(requestDto);
                    tcs.SetResult(response);
                }
                catch (Exception ex)
                {
                    HandleException(ex, (TResponse r, Exception rex) => tcs.SetException(rex));
                }
                return tcs.Task;
            }

            public Task<List<TResponse>> SendAllAsync<TResponse>(IEnumerable<IReturn<TResponse>> requests)
            {
                throw new NotImplementedException();
            }

            private static void HandleException<TResponse>(Exception exception, Action<TResponse, Exception> onError)
            {
                var response = (TResponse)typeof(TResponse).CreateInstance();
                var hasResponseStatus = response as IHasResponseStatus;
                if (hasResponseStatus != null)
                {
                    hasResponseStatus.ResponseStatus = new ResponseStatus {
                        ErrorCode = exception.GetType().Name,
                        Message = exception.Message,
                        StackTrace = exception.StackTrace,
                    };
                }
                var webServiceEx = new WebServiceException(exception.Message, exception);
                if (onError != null) onError(response, webServiceEx);
            }

            public void SetCredentials(string userName, string password)
            {
                throw new NotImplementedException();
            }

            public Task<TResponse> GetAsync<TResponse>(IReturn<TResponse> requestDto)
            {
                throw new NotImplementedException();
            }

            public Task<TResponse> GetAsync<TResponse>(object requestDto)
            {
                throw new NotImplementedException();
            }

            public Task<TResponse> GetAsync<TResponse>(string relativeOrAbsoluteUrl)
            {
                var tcs = new TaskCompletionSource<TResponse>();
                try
                {
                    var response = parent.ExecutePath<TResponse>(HttpMethods.Get, new UrlParts(relativeOrAbsoluteUrl), default(TResponse));
                    tcs.SetResult(response);
                }
                catch (Exception ex)
                {
                    HandleException(ex, (TResponse r, Exception rex) => tcs.SetException(rex));
                }
                return tcs.Task;
            }

            public Task GetAsync(IReturnVoid requestDto)
            {
                throw new NotImplementedException();
            }

            public Task<TResponse> DeleteAsync<TResponse>(IReturn<TResponse> requestDto)
            {
                throw new NotImplementedException();
            }

            public Task<TResponse> DeleteAsync<TResponse>(object requestDto)
            {
                throw new NotImplementedException();
            }

            public Task<TResponse> DeleteAsync<TResponse>(string relativeOrAbsoluteUrl)
            {
                var tcs = new TaskCompletionSource<TResponse>();
                try
                {
                    var response = parent.ExecutePath<TResponse>(HttpMethods.Delete, new UrlParts(relativeOrAbsoluteUrl), default(TResponse));
                    tcs.SetResult(response);
                }
                catch (Exception ex)
                {
                    HandleException(ex, (TResponse r, Exception rex) => tcs.SetException(rex));
                }
                return tcs.Task;
            }

            public Task DeleteAsync(IReturnVoid requestDto)
            {
                throw new NotImplementedException();
            }

            public Task<TResponse> PostAsync<TResponse>(IReturn<TResponse> requestDto)
            {
                throw new NotImplementedException();
            }

            public Task<TResponse> PostAsync<TResponse>(object requestDto)
            {
                throw new NotImplementedException();
            }

            public Task PostAsync(IReturnVoid requestDto)
            {
                throw new NotImplementedException();
            }

            public Task<TResponse> PutAsync<TResponse>(IReturn<TResponse> requestDto)
            {
                throw new NotImplementedException();
            }

            public Task<TResponse> PutAsync<TResponse>(object requestDto)
            {
                throw new NotImplementedException();
            }

            public Task PutAsync(IReturnVoid requestDto)
            {
                throw new NotImplementedException();
            }

            public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, IReturn<TResponse> requestDto)
            {
                throw new NotImplementedException();
            }

            public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, object requestDto)
            {
                throw new NotImplementedException();
            }

            public Task CustomMethodAsync(string httpVerb, IReturnVoid requestDto)
            {
                throw new NotImplementedException();
            }

            public Task<TResponse> PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request)
            {
                var tcs = new TaskCompletionSource<TResponse>();
                try
                {
                    var response = parent.ExecutePath<TResponse>(HttpMethods.Post, new UrlParts(relativeOrAbsoluteUrl), request);
                    tcs.SetResult(response);
                }
                catch (Exception ex)
                {
                    HandleException(ex, (TResponse r, Exception rex) => tcs.SetException(rex));
                }
                return tcs.Task;
            }

            public Task<TResponse> PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request)
            {
                var tcs = new TaskCompletionSource<TResponse>();
                try
                {
                    var response = parent.ExecutePath<TResponse>(HttpMethods.Put, new UrlParts(relativeOrAbsoluteUrl), request);
                    tcs.SetResult(response);
                }
                catch (Exception ex)
                {
                    HandleException(ex, (TResponse r, Exception rex) => tcs.SetException(rex));
                }
                return tcs.Task;
            }

            public void CancelAsync()
            {
                throw new NotImplementedException();
            }

            public void SendAsync<TResponse>(object requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
            {
                throw new NotImplementedException();
            }

            public void Dispose() { }
            public TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, FileInfo fileToUpload, object request, string fieldName = "upload")
            {
                throw new NotImplementedException();
            }

            public TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, object request, string fieldName = "upload")
            {
                throw new NotImplementedException();
            }
        }

        public object ExecutePath(string pathInfo)
        {
            return ExecutePath(HttpMethods.Get, pathInfo);
        }

        private class UrlParts
        {
            public UrlParts(string pathInfo)
            {
                this.PathInfo = pathInfo.UrlDecode();
                var qsIndex = pathInfo.IndexOf("?");
                if (qsIndex != -1)
                {
                    var qs = pathInfo.Substring(qsIndex + 1);
                    this.PathInfo = pathInfo.Substring(0, qsIndex);
                    var kvps = qs.Split('&');

                    this.QueryString = new Dictionary<string, string>();
                    foreach (var kvp in kvps)
                    {
                        var parts = kvp.Split('=');
                        this.QueryString[parts[0]] = parts.Length > 1 ? parts[1] : null;
                    }
                }
            }

            public string PathInfo { get; private set; }
            public Dictionary<string, string> QueryString { get; private set; }
        }

        public object ExecutePath(string httpMethod, string pathInfo)
        {
            var urlParts = new UrlParts(pathInfo);
            return ExecutePath(httpMethod, urlParts.PathInfo, urlParts.QueryString, null, null);
        }

        private TResponse ExecutePath<TResponse>(string httpMethod, UrlParts urlParts, object requestDto)
        {
            return (TResponse)ExecutePath(httpMethod, urlParts.PathInfo, urlParts.QueryString, null, requestDto);
        }

        public TResponse ExecutePath<TResponse>(string httpMethod, string pathInfo, object requestDto)
        {
            var urlParts = new UrlParts(pathInfo);
            return (TResponse)ExecutePath(httpMethod, urlParts.PathInfo, urlParts.QueryString, null, requestDto);
        }

        public object ExecutePath<T>(
            string httpMethod,
            string pathInfo,
            Dictionary<string, string> queryString,
            Dictionary<string, string> formData,
            T requestBody)
        {
            var isDefault = Equals(requestBody, default(T));
            var json = !isDefault ? JsonSerializer.SerializeToString(requestBody) : null;
            return ExecutePath(httpMethod, pathInfo, queryString, formData, json);
        }

        public object ExecutePath(
            string httpMethod,
            string pathInfo,
            Dictionary<string, string> queryString,
            Dictionary<string, string> formData,
            string requestBody)
        {
            var httpHandler = GetHandler(httpMethod, pathInfo);

            var contentType = (formData != null && formData.Count > 0)
                ? MimeTypes.FormUrlEncoded
                : requestBody != null ? MimeTypes.Json : null;

            var httpReq = new MockHttpRequest(
                    httpHandler.RequestName, httpMethod, contentType,
                    pathInfo,
                    queryString.ToNameValueCollection(),
                    requestBody == null ? null : new MemoryStream(Encoding.UTF8.GetBytes(requestBody)),
                    formData.ToNameValueCollection()
                );

            var request = httpHandler.CreateRequest(httpReq, httpHandler.RequestName);
            object response;
            try
            {
                response = httpHandler.GetResponse(httpReq, request);
            }
            catch (Exception ex)
            {
                response = DtoUtils.CreateErrorResponse(request, ex);
            }

            var httpRes = response as IHttpResult;
            if (httpRes != null)
            {
                var httpError = httpRes as IHttpError;
                if (httpError != null)
                {
                    throw new WebServiceException(httpError.Message) {
                        StatusCode = httpError.Status,
                        ResponseDto = httpError.Response
                    };
                }
                var hasResponseStatus = httpRes.Response as IHasResponseStatus;
                if (hasResponseStatus != null)
                {
                    var status = hasResponseStatus.ResponseStatus;
                    if (status != null && !status.ErrorCode.IsNullOrEmpty())
                    {
                        throw new WebServiceException(status.Message) {
                            StatusCode = (int)HttpStatusCode.InternalServerError,
                            ResponseDto = httpRes.Response,
                        };
                    }
                }

                return httpRes.Response;
            }

            return response;
        }

        public T GetRequest<T>(string pathInfo)
        {
            return (T) GetRequest(pathInfo);
        }

        public object GetRequest(string pathInfo)
        {
            var urlParts = new UrlParts(pathInfo);
            return GetRequest(HttpMethods.Get, urlParts.PathInfo, urlParts.QueryString, null, null);
        }

        public object GetRequest(string httpMethod, string pathInfo)
        {
            var urlParts = new UrlParts(pathInfo);
            return GetRequest(httpMethod, urlParts.PathInfo, urlParts.QueryString, null, null);
        }

        public object GetRequest(
                string httpMethod,
                string pathInfo,
                Dictionary<string, string> queryString,
                Dictionary<string, string> formData,
                string requestBody)
        {
            var httpHandler = GetHandler(httpMethod, pathInfo);

            var contentType = (formData != null && formData.Count > 0)
                ? MimeTypes.FormUrlEncoded
                : requestBody != null ? MimeTypes.Json : null;

            var httpReq = new MockHttpRequest(
                    httpHandler.RequestName, httpMethod, contentType,
                    pathInfo,
                    queryString.ToNameValueCollection(),
                    requestBody == null ? null : new MemoryStream(Encoding.UTF8.GetBytes(requestBody)),
                    formData.ToNameValueCollection()
                );

            var request = httpHandler.CreateRequest(httpReq, httpHandler.RequestName);
            return request;
        }

        private static ServiceStackHandlerBase GetHandler(string httpMethod, string pathInfo)
        {
            var httpHandler = HttpHandlerFactory.GetHandlerForPathInfo(httpMethod, pathInfo, pathInfo, null) as ServiceStackHandlerBase;
            if (httpHandler == null)
                throw new NotSupportedException(pathInfo);
            return httpHandler;
        }
    }

}