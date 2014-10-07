/*
using System;
using System.Collections.Generic;
using System.Net;
using Funq;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface.Testing
{
    public class MockRequestContext : IRequestContext
    {
        public MockRequestContext()
        {
            this.Cookies = new Dictionary<string, Cookie>();
            this.Files = new IFile[0];
            this.Container = new Container();
            var httpReq = new MockHttpRequest { Container = this.Container };
            httpReq.AddSessionCookies();
            this.Container.Register<IHttpRequest>(httpReq);
            var httpRes = new MockHttpResponse();
            this.Container.Register<IHttpResponse>(httpRes);
            httpReq.Container = this.Container;
        }

        public T Get<T>() where T : class
        {
            return Container.TryResolve<T>();
        }

        public string IpAddress { get; private set; }

        public string GetHeader(string headerName)
        {
            return Get<IHttpRequest>().Headers[headerName];
        }

        public Container Container { get; private set; }
        public IDictionary<string, Cookie> Cookies { get; private set; }
        public EndpointAttributes EndpointAttributes { get; private set; }
        public IRequestAttributes RequestAttributes { get; private set; }
        public string ContentType { get; private set; }
        public string ResponseContentType { get; set; }
        public string CompressionType { get; private set; }
        public string AbsoluteUri { get; private set; }
        public string PathInfo { get; private set; }
        public IFile[] Files { get; private set; }

        public AuthUserSession RemoveSession()
        {
            var httpReq = this.Get<IHttpRequest>();
            httpReq.RemoveSession();
            return httpReq.GetSession() as AuthUserSession;
        }

        public AuthUserSession ReloadSession()
        {
            var httpReq = this.Get<IHttpRequest>();
            return httpReq.GetSession() as AuthUserSession;
        }

        public void Dispose()
        {
        }

        public IAppHost CreateAppHost()
        {
            return new TestAppHost(this.Container);
        }
    }
}
*/
