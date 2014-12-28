using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.Formats;
using ServiceStack.Html;
using ServiceStack.Messaging;
using ServiceStack.MiniProfiler;
using ServiceStack.Redis;
using ServiceStack.Web;
using IHtmlString = System.Web.IHtmlString;

namespace ServiceStack.Razor
{
    /// <summary>
    /// Class to represent attribute values and, more importantly, 
    /// decipher them from tuple madness slightly.
    /// </summary>
    public class AttributeValue
    {
        public Tuple<string, int> Prefix { get; private set; }

        public Tuple<object, int> Value { get; private set; }

        public bool IsLiteral { get; private set; }

        public AttributeValue(Tuple<string, int> prefix, Tuple<object, int> value, bool isLiteral)
        {
            this.Prefix = prefix;
            this.Value = value;
            this.IsLiteral = isLiteral;
        }

        public static implicit operator AttributeValue(Tuple<Tuple<string, int>, Tuple<object, int>, bool> value)
        {
            return new AttributeValue(value.Item1, value.Item2, value.Item3);
        }

        public static implicit operator AttributeValue(Tuple<Tuple<string, int>, Tuple<string, int>, bool> value)
        {
            return new AttributeValue(
                value.Item1, new Tuple<object, int>(value.Item2.Item1, value.Item2.Item2), value.Item3);
        }
    }

    //Should handle all razor rendering functionality
    public abstract class RenderingPage
    {
        public IRequest Request { get; set; }

        public IResponse Response { get; set; }

        public virtual StreamWriter Output { get; set; }

        public dynamic ViewBag { get; set; }

        public IViewBag TypedViewBag
        {
            get { return (IViewBag)ViewBag; }
        }

        public IRazorView ParentPage { get; set; }
        public IRazorView ChildPage { get; set; }
        public string ChildBody { get; set; }

        public Dictionary<string, Action> childSections = new Dictionary<string, Action>();

        protected RenderingPage()
        {
            this.ViewBag = new DynamicDictionary(this);
        }

        //overridden by the RazorEngine when razor generates code.
        public abstract void Execute();

        //No HTML encoding
        public virtual void WriteLiteral(string str)
        {
            this.Output.Write(str);
        }

        //With HTML encoding
        public virtual void Write(object obj)
        {
            this.Output.Write(HtmlEncode(obj));
        }

        //With HTML encoding
        public virtual void WriteTo(TextWriter writer, object obj)
        {
            writer.Write(HtmlEncode(obj));
        }

        public virtual void WriteTo(TextWriter writer, HelperResult value)
        {
            if (value != null)
            {
                value.WriteTo(writer);
            }
        }

        public virtual void WriteLiteralTo(TextWriter writer, HelperResult value)
        {
            if (value != null)
            {
                value.WriteTo(writer);
            }
        }

        public void WriteLiteralTo(TextWriter writer, string literal)
        {
            if (literal == null)
                return;

            writer.Write(literal);
        }

        private static string HtmlEncode(object value)
        {
            if (value == null)
            {
                return null;
            }

            var str = value as System.Web.IHtmlString;

            return str != null ? str.ToHtmlString() : HttpUtility.HtmlEncode(Convert.ToString(value, CultureInfo.CurrentCulture));
        }

        public virtual void WriteAttribute(string name, Tuple<string, int> prefix, Tuple<string, int> suffix, params AttributeValue[] values)
        {
            var attributeValue = this.BuildAttribute(name, prefix, suffix, values);
            this.WriteLiteral(attributeValue);
        }

        public virtual void WriteAttributeTo(TextWriter writer, string name, Tuple<string, int> prefix, Tuple<string, int> suffix, params AttributeValue[] values)
        {
            var attributeValue = this.BuildAttribute(name, prefix, suffix, values);
            WriteLiteralTo(writer, attributeValue);
        }

        private string BuildAttribute(string name, Tuple<string, int> prefix, Tuple<string, int> suffix,
                                      params AttributeValue[] values)
        {
            var writtenAttribute = false;
            var attributeBuilder = new StringBuilder(prefix.Item1);

            foreach (var value in values)
            {
                if (this.ShouldWriteValue(value.Value.Item1))
                {
                    var stringValue = this.GetStringValue(value);
                    var valuePrefix = value.Prefix.Item1;

                    if (!string.IsNullOrEmpty(valuePrefix))
                    {
                        attributeBuilder.Append(valuePrefix);
                    }

                    attributeBuilder.Append(stringValue);
                    writtenAttribute = true;
                }
            }

            attributeBuilder.Append(suffix.Item1);

            var renderAttribute = writtenAttribute || values.Length == 0;

            if (renderAttribute)
            {
                return attributeBuilder.ToString();
            }

            return string.Empty;
        }

        private string GetStringValue(AttributeValue value)
        {
            if (value.IsLiteral)
            {
                return (string)value.Value.Item1;
            }

            var htmlString = value.Value.Item1 as IHtmlString;
            if (htmlString != null)
                return htmlString.ToHtmlString();

            //if (value.Value.Item1 is DynamicDictionaryValue) {
            //    var dynamicValue = (DynamicDictionaryValue)value.Value.Item1;
            //    return dynamicValue.HasValue ? dynamicValue.Value.ToString() : string.Empty;
            //}

            return value.Value.Item1.ToString();
        }


        private bool ShouldWriteValue(object value)
        {
            if (value == null)
            {
                return false;
            }

            if (value is bool)
            {
                var boolValue = (bool)value;

                return boolValue;
            }

            return true;
        }

        public void SetChildPage(IRazorView childPage, string childBody)
        {
            this.ChildPage = childPage;
            this.ChildBody = childBody;
        }

        public object RenderBody()
        {
            if (ChildBody != null)
            {
                Output.Write(ChildBody);
            }

            return null;
        }

        public virtual bool IsSectionDefined(string sectionName)
        {
            var parentPage = ParentPage as RenderingPage;
            return parentPage != null
                ? parentPage.IsChildSectionDefined(sectionName)
                : IsChildSectionDefined(sectionName);
        }

        internal virtual bool IsChildSectionDefined(string sectionName)
        {
            var hasChildSection = this.childSections.ContainsKey(sectionName);
            if (hasChildSection) return true;

            var childPage = ChildPage as RenderingPage;
            return childPage != null && childPage.IsSectionDefined(sectionName);
        }

        public virtual void DefineSection(string sectionName, Action action)
        {
            this.childSections.Add(sectionName, action);
        }

        public object RenderSection(string sectionName, bool required)
        {
            if (required && !IsSectionDefined(sectionName) && !ChildPage.IsSectionDefined(sectionName))
                throw new Exception("Required Section {0} is not defined".Fmt(sectionName));

            return RenderSection(sectionName);
        }

        public object RenderSection(string sectionName)
        {
            var parentPage = ParentPage as RenderingPage;
            return parentPage != null
                ? parentPage.RenderChildSection(sectionName)
                : RenderChildSection(sectionName);
        }

        internal object RenderChildSection(string sectionName)
        {
            Action section;
            if (childSections.TryGetValue(sectionName, out section))
            {
                section();
                return null;
            }

            var childPage = ChildPage as RenderingPage;
            if (childPage != null)
            {
                childPage.RenderChildSection(sectionName, Output);
            }
            return null;
        }

        public void RenderChildSection(string sectionName, StreamWriter writer)
        {
            Action section;
            if (childSections.TryGetValue(sectionName, out section))
            {
                var hold = Output;
                try
                {
                    Output = writer;
                    section();
                    Output.Flush();
                }
                finally
                {
                    Output = hold;
                }
            }
        }
    }

    public interface IHasModel
    {
        Type ModelType { get; }

        void SetModel(object o);
    }

    public abstract class ViewPageBase<TModel> : RenderingPage, IHasModel
    {
        public string Layout
        {
            get
            {
                return layout;
            }
            set
            {
                layout = value != null ? value.Trim(' ', '"') : null;
            }
        }

        private TModel model;
        public TModel Model
        {
            get { return model; }
            set
            {
                SetModel(value);
            }
        }

        public abstract Type ModelType { get; }

        public virtual void SetModel(object o)
        {
            var viewModel = o is TModel ? (TModel)o : default(TModel);
            this.model = viewModel;

            if (Equals(viewModel, default(TModel)))
            {
                this.ModelError = o;
            }
        }

        public UrlHelper Url = new UrlHelper();

        private IAppHost appHost;

        public virtual IViewEngine ViewEngine { get; set; }

        public IAppHost AppHost
        {
            get { return appHost ?? ServiceStackHost.Instance; }
            set { appHost = value; }
        }

        public IAppSettings AppSettings
        {
            get { return AppHost.AppSettings; }
        }

        public virtual T Get<T>()
        {
            return this.AppHost.TryResolve<T>();
        }

        public virtual T GetPlugin<T>() where T : class, IPlugin
        {
            return this.AppHost.GetPlugin<T>();
        }

        public virtual T TryResolve<T>()
        {
            return this.AppHost.TryResolve<T>();
        }

        public virtual T ResolveService<T>()
        {
            var service = Get<T>();
            var requiresContext = service as IRequiresRequest;
            if (requiresContext != null)
            {
                requiresContext.Request = this.Request;
            }
            return service;
        }

        public virtual object ExecuteService<T>(Func<T, object> fn)
        {
            var service = ResolveService<T>();
            using (service as IDisposable)
            {
                return fn(service);
            }
        }

        public bool IsError
        {
            get { return ModelError != null; }
        }

        public object ModelError { get; set; }

        private ICacheClient cache;
        public ICacheClient Cache
        {
            get { return cache ?? (cache = AppHost.GetCacheClient()); }
        }

        private IDbConnection db;
        public IDbConnection Db
        {
            get { return db ?? (db = Get<IDbConnectionFactory>().OpenDbConnection()); }
        }

        private IRedisClient redis;
        public IRedisClient Redis
        {
            get { return redis ?? (redis = Get<IRedisClientsManager>().GetClient()); }
        }

        private IMessageProducer messageProducer;
        public virtual IMessageProducer MessageProducer
        {
            get { return messageProducer ?? (messageProducer = Get<IMessageFactory>().CreateMessageProducer()); }
        }

        private ISessionFactory sessionFactory;
        private ISession session;
        public virtual ISession SessionBag
        {
            get
            {
                if (sessionFactory == null)
                    sessionFactory = new SessionFactory(Cache);

                return session ?? (session = sessionFactory.GetOrCreateSession(Request, Response));
            }
        }

        private string layout;

        public virtual IAuthSession GetSession(bool reload = false)
        {
            var req = this.Request;
            if (req.GetSessionId() == null)
                req.Response.CreateSessionIds(req);
            return req.GetSession(reload);
        }

        public virtual T SessionAs<T>() where T : class, IAuthSession
        {
            return SessionFeature.GetOrCreateSession<T>(Cache, Request, Response);
        }

        public bool IsAuthenticated
        {
            get { return this.GetSession().IsAuthenticated; }
        }

        public string SessionKey
        {
            get
            {
                return SessionFeature.GetSessionKey();
            }
        }

        public void ClearSession()
        {
            this.Cache.Remove(SessionKey);
        }

        public virtual void Dispose()
        {
            try
            {
                if (this.ChildPage != null) this.ChildPage.Dispose();
                this.ChildPage = null;
            }
            catch { }
            try
            {
                if (cache != null) cache.Dispose();
                cache = null;
            }
            catch { }
            try
            {
                if (db != null) db.Dispose();
                db = null;
            }
            catch { }
            try
            {
                if (redis != null) redis.Dispose();
                redis = null;
            }
            catch { }
            try
            {
                if (messageProducer != null) messageProducer.Dispose();
                messageProducer = null;
            }
            catch { }
        }

        public string Href(string url)
        {
            var replacedUrl = Url.Content(url);
            return replacedUrl;
        }

        public void Prepend(string contents)
        {
            if (contents == null) return;
            //Builder.Insert(0, contents);
        }

        public bool IsPostBack
        {
            get { return this.Request.Verb == HttpMethods.Post; }
        }

        public ResponseStatus GetErrorStatus()
        {
            var errorStatus = this.Request.GetItem(HtmlFormat.ErrorStatusKey);
            return errorStatus as ResponseStatus;
        }

        public MvcHtmlString GetErrorMessage()
        {
            var errorStatus = GetErrorStatus();
            return errorStatus == null ? null : MvcHtmlString.Create(errorStatus.Message);
        }

        public MvcHtmlString GetAbsoluteUrl(string virtualPath)
        {
            return MvcHtmlString.Create(AppHost.ResolveAbsoluteUrl(virtualPath, Request));
        }

        public void ApplyRequestFilters(object requestDto)
        {
            HostContext.ApplyRequestFilters(base.Request, base.Response, requestDto);
            if (base.Response.IsClosed)
                throw new StopExecutionException();
        }

        public void RedirectIfNotAuthenticated(string redirectUrl=null)
        {
            if (IsAuthenticated) return;

            redirectUrl = redirectUrl
                ?? AuthenticateService.HtmlRedirect
                ?? HostContext.Config.DefaultRedirectPath
                ?? HostContext.Config.WebHostUrl
                ?? "/";
            AuthenticateAttribute.DoHtmlRedirect(redirectUrl, Request, Response, includeRedirectParam: true);
            throw new StopExecutionException();
        }

        public bool RenderErrorIfAny()
        {
            if (!IsError) return false;

            var responseStatus = GetErrorStatus();
            var stackTrace = responseStatus.StackTrace != null
                ? "<pre>" + responseStatus.StackTrace + "</pre>"
                : "";

            WriteLiteral(@"
            <div id=""error-response"" class=""alert alert-danger"">
                <h4>" + 
                    responseStatus.ErrorCode + ": " + 
                    responseStatus.Message + @"
                </h4>" + 
                stackTrace + 
            "</div>");

            return true;
        }
    }
}