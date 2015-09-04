using System;
using System.Data;
using System.IO;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Messaging;
using ServiceStack.Web;

namespace ServiceStack.Razor
{
    // Dummy class to satisfy linked files from SS.Razor project
    public abstract class ViewPage : ViewPageBase<dynamic>
    {
    }

    // Dummy class to satisfy linked files from SS.Razor project
    public abstract class ViewPageBase<TModel>
    {
        public string Layout { get; set; }

        public IRequest Request { get; set; }

        public IResponse Response { get; set; }

        public IAppSettings AppSettings { get; set; }

        public virtual StreamWriter Output { get; set; }

        public TModel Model { get; set; }

        public Type ModelType { get; }

        public virtual T Get<T>()
        {
            return default(T);
        }

        public virtual T GetPlugin<T>() where T : class
        {
            return default(T);
        }

        public virtual T TryResolve<T>()
        {
            return default(T);
        }

        public virtual T ResolveService<T>()
        {
            return default(T);
        }

        public virtual object ExecuteService<T>(Func<T, object> fn)
        {
            return null;
        }

        public bool IsError { get; set; }

        public object ModelError { get; set; }

        public ICacheClient Cache { get; set; }

        public IDbConnection Db { get; set; }

        public virtual IMessageProducer MessageProducer { get; set; }

        public virtual ISession SessionBag { get; set; }

        public virtual object GetSession(bool reload = false)
        {
            return null;
        }

        public virtual T SessionAs<T>() where T : class
        {
            return default(T);
        }

        public bool IsAuthenticated { get; set; }

        public string SessionKey { get; set; }

        public void ClearSession() { }

        public bool IsPostBack { get; set; }

        public ResponseStatus GetErrorStatus()
        {
            return null;
        }

        public string GetErrorMessage()
        {
            return null;
        }

        public string GetAbsoluteUrl(string virtualPath)
        {
            return null;
        }

        public void ApplyRequestFilters(object requestDto) {}

        public void RedirectIfNotAuthenticated(string redirectUrl = null) {}

        public bool RenderErrorIfAny()
        {
            return false;
        }
    }
}
