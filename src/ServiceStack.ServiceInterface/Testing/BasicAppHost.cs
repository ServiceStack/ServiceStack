using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Funq;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface.Testing
{
    public class BasicAppHost : IAppHost
    {
        public BasicAppHost()
        {
            this.Container = new Container();
            this.RequestFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
            this.ResponseFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
            this.HtmlProviders = new List<StreamSerializerResolverDelegate>();
            this.CatchAllHandlers = new List<HttpHandlerResolverDelegate>();
        }

        public void RegisterAs<T, TAs>() where T : TAs
        {
            this.Container.RegisterAs<T, TAs>();
        }

        public virtual void Release(object instance) { }
        
        public void OnEndRequest() {}

        public void Register<T>(T instance)
        {
            this.Container.Register(instance);
        }

        public T TryResolve<T>()
        {
            return this.Container.TryResolve<T>();
        }

        public Container Container { get; set; }

        public IContentTypeFilter ContentTypeFilters { get; set; }

        public List<Action<IHttpRequest, IHttpResponse, object>> RequestFilters { get; set; }

        public List<Action<IHttpRequest, IHttpResponse, object>> ResponseFilters { get; set; }

        public List<StreamSerializerResolverDelegate> HtmlProviders { get; set; }

        public List<HttpHandlerResolverDelegate> CatchAllHandlers { get; set; }

        public Dictionary<Type, Func<IHttpRequest, object>> RequestBinders
        {
            get { throw new NotImplementedException(); }
        }

        public EndpointHostConfig Config { get; set; }

        public void RegisterService(Type serviceType, params string[] atRestPaths)
        {
            if (Config == null)
                Config = new EndpointHostConfig("BasicAppHost", new ServiceManager(Assembly.GetExecutingAssembly()));				

            Config.ServiceManager.RegisterService(serviceType);
        }

        public void LoadPlugin(params IPlugin[] plugins)
        {
            plugins.ToList().ForEach(x => x.Register(this));
        }
    }
}