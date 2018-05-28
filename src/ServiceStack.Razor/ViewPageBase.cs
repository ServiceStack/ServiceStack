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
using ServiceStack.IO;
using ServiceStack.Messaging;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Text;
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

        public static Action<RenderingPage, string> WriteLiteralFn = DefaultWriteLiteral;
        public static Action<RenderingPage, TextWriter, string> WriteLiteralToFn = DefaultWriteLiteralTo;

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

        public static void DefaultWriteLiteral(RenderingPage page, string str)
        {
            page.Output.Write(str);
        }

        public static void DefaultWriteLiteralTo(RenderingPage page, TextWriter writer, string str)
        {
            writer.Write(str);
        }

        //No HTML encoding
        public virtual void WriteLiteral(string str)
        {
            WriteLiteralFn(this, str);
        }

        //With HTML encoding
        public virtual void Write(object obj)
        {
            WriteLiteralFn(this, HtmlEncode(obj));
        }

        //With HTML encoding
        public virtual void WriteTo(TextWriter writer, object obj)
        {
            WriteLiteralToFn(this, writer, HtmlEncode(obj));
        }

        public virtual void WriteTo(TextWriter writer, HelperResult value)
        {
            value?.WriteTo(writer);
        }

        public virtual void WriteLiteralTo(TextWriter writer, HelperResult value)
        {
            value?.WriteTo(writer);
        }

        public void WriteLiteralTo(TextWriter writer, string literal)
        {
            if (literal == null)
                return;

            WriteLiteralToFn(this, writer, literal);
        }

        private static string HtmlEncode(object value)
        {
            if (value == null)
            {
                return null;
            }

            return value is System.Web.IHtmlString str ? str.ToHtmlString() : HttpUtility.HtmlEncode(Convert.ToString(value, CultureInfo.CurrentCulture));
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
            var attributeBuilder = StringBuilderCache.Allocate().Append(prefix.Item1);

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
                return StringBuilderCache.ReturnAndFree(attributeBuilder);

            StringBuilderCache.Free(attributeBuilder);

            return string.Empty;
        }

        private string GetStringValue(AttributeValue value)
        {
            if (value.IsLiteral)
            {
                return (string)value.Value.Item1;
            }

            if (value.Value.Item1 is IHtmlString htmlString)
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
            return ParentPage is RenderingPage parentPage
                ? parentPage.IsChildSectionDefined(sectionName)
                : IsChildSectionDefined(sectionName);
        }

        internal virtual bool IsChildSectionDefined(string sectionName)
        {
            var hasChildSection = this.childSections.ContainsKey(sectionName);
            if (hasChildSection) return true;

            return ChildPage is RenderingPage childPage && childPage.IsSectionDefined(sectionName);
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
            return ParentPage is RenderingPage parentPage
                ? parentPage.RenderChildSection(sectionName)
                : RenderChildSection(sectionName);
        }

        internal object RenderChildSection(string sectionName)
        {
            if (childSections.TryGetValue(sectionName, out var section))
            {
                section();
                return null;
            }

            if (ChildPage is RenderingPage childPage)
            {
                childPage.RenderChildSection(sectionName, Output);
            }
            return null;
        }

        public void RenderChildSection(string sectionName, StreamWriter writer)
        {
            if (childSections.TryGetValue(sectionName, out var section))
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
            get => layout;
            set => layout = value?.Trim(' ', '"');
        }

        private TModel model;
        public TModel Model
        {
            get => model;
            set => SetModel(value);
        }

        public abstract Type ModelType { get; }

        public virtual void SetModel(object o)
        {
            var viewModel = o is TModel m ? m : default(TModel);
            this.model = viewModel;

            if (Equals(viewModel, default(TModel)))
            {
                this.ModelError = o;
            }
        }

        public UrlHelper Url = new UrlHelper();

        public virtual IViewEngine ViewEngine { get; set; }

        private IAppHost appHost;
        public IAppHost AppHost
        {
            get => appHost ?? ServiceStackHost.Instance;
            set => appHost = value;
        }

        public bool DebugMode => HostContext.DebugMode;

        public IAppSettings AppSettings => AppHost.AppSettings;

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
            if (service is IRequiresRequest requiresContext)
            {
                requiresContext.Request = this.Request;
            }
            return service;
        }

        private IServiceGateway gateway;
        public virtual IServiceGateway Gateway => gateway ?? (gateway = HostContext.AppHost.GetServiceGateway(Request));

        public bool IsError => ModelError != null || GetErrorStatus() != null;

        public object ModelError { get; set; }

        public IVirtualFiles VirtualFiles => HostContext.VirtualFiles;
        public IVirtualPathProvider VirtualFileSources => HostContext.VirtualFileSources;

        private ICacheClient cache;
        public ICacheClient Cache => cache ?? (cache = HostContext.AppHost.GetCacheClient(Request));

        private IDbConnection db;
        public IDbConnection Db => db ?? (db = HostContext.AppHost.GetDbConnection(Request));

        private IRedisClient redis;
        public IRedisClient Redis => redis ?? (redis = HostContext.AppHost.GetRedisClient(Request));

        private IMessageProducer messageProducer;
        public virtual IMessageProducer MessageProducer => messageProducer ?? (messageProducer = HostContext.AppHost.GetMessageProducer(Request));

        private IAuthRepository authRepository;
        public IAuthRepository AuthRepository => authRepository ?? (authRepository = HostContext.AppHost.GetAuthRepository(Request));

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

        public bool IsAuthenticated => this.GetSession().IsAuthenticated;

        public string SessionKey => SessionFeature.GetSessionKey();

        public void ClearSession()
        {
            this.Cache.Remove(SessionKey);
        }

        public virtual void Dispose()
        {
            try
            {
                this.ChildPage?.Dispose();
                this.ChildPage = null;
            }
            catch { }
            try
            {
                cache?.Dispose();
                cache = null;
            }
            catch { }
            try
            {
                db?.Dispose();
                db = null;
            }
            catch { }
            try
            {
                redis?.Dispose();
                redis = null;
            }
            catch { }
            try
            {
                messageProducer?.Dispose();
                messageProducer = null;
            }
            catch { }
            try
            {
                using (authRepository as IDisposable) { }
                authRepository = null;
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

        public bool IsPostBack => this.Request.Verb == HttpMethods.Post;

        public ResponseStatus GetErrorStatus()
        {
            var errorStatus = this.Request.GetItem(HtmlFormat.ErrorStatusKey);
            return errorStatus as ResponseStatus 
                ?? GetResponseStatus(ModelError);
        }
        
        private static ResponseStatus GetResponseStatus(object response)
        {
            if (response == null)
                return null;

            if (response is ResponseStatus status)
                return status;

            if (response is IHasResponseStatus hasResponseStatus)
                return hasResponseStatus.ResponseStatus;

            var propertyInfo = response.GetType().GetProperty("ResponseStatus");
            if (propertyInfo == null)
                return null;

            return propertyInfo.GetProperty(response) as ResponseStatus;
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
            HostContext.ApplyRequestFiltersAsync(base.Request, base.Response, requestDto).Wait();
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
            var html = GetErrorHtml(GetErrorStatus());
            if (html == null)
                return false;

            WriteLiteral(html);

            return true;
        }

        public MvcHtmlString GetErrorHtml()
        {
            return MvcHtmlString.Create(GetErrorHtml(GetErrorStatus()) ?? "");
        }

        private string GetErrorHtml(ResponseStatus responseStatus)
        {
            if (responseStatus == null) return null;

            var stackTrace = responseStatus.StackTrace != null
                ? "<pre>" + responseStatus.StackTrace + "</pre>"
                : "";

            var html = @"
                <div id=""error-response"" class=""alert alert-danger"">
                    <h4>" +
                        responseStatus.ErrorCode + ": " +
                        responseStatus.Message + @"
                    </h4>" +
                    stackTrace +
                "</div>";
            return html;
        }

        public IOrmLiteDialectProvider DialectProvider => OrmLiteConfig.DialectProvider;
    }
}