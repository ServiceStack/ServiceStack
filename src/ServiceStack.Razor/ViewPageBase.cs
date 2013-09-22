using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;
using ServiceStack.Caching;
using ServiceStack.Common;
using ServiceStack.Data;
using ServiceStack.Html;
using ServiceStack.Messaging;
using ServiceStack.MiniProfiler;
using ServiceStack.Redis;
using ServiceStack.Server;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
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
        public IHttpRequest Request { get; set; }

        public IHttpResponse Response { get; set; }

        public StreamWriter Output { get; set; }

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
            return this.childSections.ContainsKey(sectionName);
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
            Action section;
            if (childSections.TryGetValue(sectionName, out section))
            {
                section();
            }
            else if (this.ChildPage != null)
            {
                this.ChildPage.RenderSection(sectionName, Output);
            }
            return null;
        }
        
        public void RenderSection(string sectionName, StreamWriter writer)
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

        public TModel Model { get; set; }
        public abstract Type ModelType { get; }
        
        public virtual void SetModel(object o)
        {
            var viewModel = o is TModel ? (TModel)o : default(TModel);
            this.Model = viewModel;

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
            get { return appHost ?? EndpointHost.AppHost; }
            set { appHost = value; }
        }

        public T Get<T>()
        {
            return this.AppHost.TryResolve<T>();
        }

        public object ModelError { get; set; }

        public ResponseStatus ResponseStatus
        {
            get
            {
                return ToResponseStatus(ModelError) ?? ToResponseStatus(Model);
            }
        }

        private ResponseStatus ToResponseStatus<T>(T modelError)
        {
            var ret = modelError.GetResponseStatus();
            if (ret != null) return ret;

            if (modelError is DynamicObject)
            {
                var dynError = modelError as dynamic;
                return (ResponseStatus)dynError.ResponseStatus;
            }

            return null;
        }

        private ICacheClient cache;
        public ICacheClient Cache
        {
            get { return cache ?? (cache = Get<ICacheClient>()); }
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
        public virtual ISession Session
        {
            get
            {
                if (sessionFactory == null)
                    sessionFactory = new SessionFactory(Cache);

                return session ?? (session = sessionFactory.GetOrCreateSession(Request, Response));
            }
        }

        private IAuthSession userSession;
        private string layout;

        public virtual T GetSession<T>() where T : class, IAuthSession
        {
            if (userSession != null) return (T)userSession;
            return (T)(userSession = SessionFeature.GetOrCreateSession<T>(Cache, Request, Response));
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
            userSession = null;
            this.Cache.Remove(SessionKey);
        }

        public virtual void Dispose()
        {
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
    }
}