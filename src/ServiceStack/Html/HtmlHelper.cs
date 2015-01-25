// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using ServiceStack.Formats;
using ServiceStack.Host;
using ServiceStack.Html.AntiXsrf;
using ServiceStack.Support.Markdown;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Html
{
	public class HtmlHelper
    {
        public const string DefaultTemplate = null;
        public const string EmptyTemplate = "";

        public static string ValidationMessageCssClassNames = "help-block error";
        public static string ValidationSummaryCssClassNames = "error-summary alert alert-danger";
        public static string ValidationSuccessCssClassNames = "alert alert-success";
        public static readonly string ValidationInputCssClassName = "error";

        public static readonly string ValidationInputValidCssClassName = "input-validation-valid";
        public static readonly string ValidationMessageValidCssClassName = "field-validation-valid";
        public static readonly string ValidationSummaryValidCssClassName = "validation-summary-valid";
        private DynamicViewDataDictionary viewBag;

        public static List<Type> HtmlExtensions = new List<Type> 
		{
			typeof(DisplayTextExtensions),
			typeof(InputExtensions),
			typeof(LabelExtensions),
			typeof(TextAreaExtensions),
            typeof(SelectExtensions)
		};

		public static MethodInfo GetMethod(string methodName)
		{
			foreach (var htmlExtension in HtmlExtensions)
			{
				var mi = htmlExtension.GetMethods().ToList()
					.FirstOrDefault(x => x.Name == methodName);

				if (mi != null) return mi;
			}
			return null;
		}

		private delegate string HtmlEncoder(object value);
		private static readonly HtmlEncoder htmlEncoder = GetHtmlEncoder();

		internal bool RenderHtml { get; private set; }

        public IHttpRequest HttpRequest { get; set; }
        public IHttpResponse HttpResponse { get; set; }
        public StreamWriter Writer { get; set; }
        public IViewEngine ViewEngine { get; set; }

        public IRazorView RazorPage { get; protected set; }
        public MarkdownPage MarkdownPage { get; protected set; }
		public Dictionary<string, object> ScopeArgs { get; protected set; }
	    private ViewDataDictionary viewData;

        public void Init(IViewEngine viewEngine, IRequest httpReq, IResponse httpRes, IRazorView razorPage, 
            Dictionary<string, object> scopeArgs = null, ViewDataDictionary viewData = null)
        {
            ViewEngine = viewEngine;
            HttpRequest = httpReq as IHttpRequest;
            HttpResponse = httpRes as IHttpResponse;
            RazorPage = razorPage;
            //ScopeArgs = scopeArgs;
            this.viewData = viewData;
        }

	    private static int counter = 0;
        private int id = 0;

	    public HtmlHelper()
	    {
            this.RenderHtml = true;
            id = counter++;
	    }

        public void Init(MarkdownPage markdownPage, Dictionary<string, object> scopeArgs,
            bool renderHtml, ViewDataDictionary viewData, HtmlHelper htmlHelper)
		{
            Init(null, null, markdownPage.Markdown, viewData, htmlHelper);

            this.RenderHtml = renderHtml;
			this.MarkdownPage = markdownPage;
			this.ScopeArgs = scopeArgs;
		}

        public void Init(IHttpRequest httpReq, IHttpResponse httpRes, IViewEngine viewEngine, ViewDataDictionary viewData, HtmlHelper htmlHelper)
		{
            this.RenderHtml = true;
            this.HttpRequest = httpReq ?? (htmlHelper != null ? htmlHelper.HttpRequest : null);
            this.HttpResponse = httpRes ?? (htmlHelper != null ? htmlHelper.HttpResponse : null);
            this.ViewEngine = viewEngine;
			this.ViewData = viewData;
			this.ViewData.PopulateModelState();
		}

		public MvcHtmlString Partial(string viewName)
		{
		    return Partial(viewName, null);
		}
		
		public MvcHtmlString Partial(string viewName, object model)
		{
		    var masterModel = this.viewData;
            try
            {
                this.viewData = new ViewDataDictionary(model);
                var result = ViewEngine.RenderPartial(viewName, model, this.RenderHtml, Writer, this);
                return MvcHtmlString.Create(result);
            }
            finally
            {
                this.viewData = masterModel;
            }
        }

        public MvcHtmlString RenderAction(string route, string viewName = null)
        {
            var req = new BasicRequest {
                Verb = HttpMethods.Get,
                PathInfo = route,
                ContentType = MimeTypes.Html,
                ResponseContentType = MimeTypes.Html,
            }.PopulateWith(HttpRequest);
    
            req.SetTemplate(EmptyTemplate);

            if (viewName != null)
                req.SetView(viewName);

            var response = HostContext.ServiceController.Execute(req);

            req.Response.WriteToResponse(req, response)
                .Wait(); //unnecessary as results are sync anyway

            var resBytes = ((MemoryStream)req.Response.OutputStream).ToArray();
            var html = resBytes.FromUtf8Bytes();

            return MvcHtmlString.Create(html);
        }

        public string Debug(object model)
        {
            if (model != null)
            {
                model.PrintDump();
            }

            return null;
        }

        public static bool ClientValidationEnabled
        {
            get { return ViewContext.GetClientValidationEnabled(); }
            set { ViewContext.SetClientValidationEnabled(value); }
        }

        internal Func<string, ModelMetadata, IEnumerable<ModelClientValidationRule>> ClientValidationRuleFactory { get; set; }

        public static bool UnobtrusiveJavaScriptEnabled
        {
            get { return ViewContext.GetUnobtrusiveJavaScriptEnabled(); }
            set { ViewContext.SetUnobtrusiveJavaScriptEnabled(value); }
        }

        public dynamic ViewBag
        {
            get { return viewBag ?? (viewBag = new DynamicViewDataDictionary(() => ViewData)); }
        }

        public ViewContext ViewContext { get; private set; }

	    public ViewDataDictionary ViewData
	    {
	        get { return viewData ?? (viewData = new ViewDataDictionary()); }
	        protected set { viewData = value; }
	    }

        public void SetModel(object model)
        {
			ViewData.Model = model;
        }

        public IViewDataContainer ViewDataContainer { get; internal set; }

		public static RouteValueDictionary AnonymousObjectToHtmlAttributes(object htmlAttributes)
		{
			var result = new RouteValueDictionary();

			if (htmlAttributes != null)
			{
				foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(htmlAttributes))
				{
					result.Add(property.Name.Replace('_', '-'), property.GetValue(htmlAttributes));
				}
			}

			return result;
		}

        public MvcHtmlString AntiForgeryToken()
        {
            return MvcHtmlString.Create(AntiForgery.GetHtml().ToString());
        }

        public MvcHtmlString AntiForgeryToken(string salt)
        {
            if (!String.IsNullOrEmpty(salt)) {
                throw new NotSupportedException("This method is deprecated. Use the AntiForgeryToken() method instead. To specify custom data to be embedded within the token, use the static AntiForgeryConfig.AdditionalDataProvider property.");
            }

            return AntiForgeryToken();
        }

        public MvcHtmlString AntiForgeryToken(string salt, string domain, string path)
        {
            if (!String.IsNullOrEmpty(salt) || !String.IsNullOrEmpty(domain) || !String.IsNullOrEmpty(path)) {
                throw new NotSupportedException("This method is deprecated. Use the AntiForgeryToken() method instead. To specify a custom domain for the generated cookie, use the <httpCookies> configuration element. To specify custom data to be embedded within the token, use the static AntiForgeryConfig.AdditionalDataProvider property.");
            }

            return AntiForgeryToken();
        }

		public string AttributeEncode(string value)
		{
			return !string.IsNullOrEmpty(value) ? HttpUtility.HtmlAttributeEncode(value) : String.Empty;
		}

		public string AttributeEncode(object value)
		{
			return AttributeEncode(Convert.ToString(value, CultureInfo.InvariantCulture));
		}

        public void EnableClientValidation()
        {
            EnableClientValidation(enabled: true);
        }

        public void EnableClientValidation(bool enabled)
        {
            ViewContext.ClientValidationEnabled = enabled;
        }

        public void EnableUnobtrusiveJavaScript()
        {
            EnableUnobtrusiveJavaScript(enabled: true);
        }

        public void EnableUnobtrusiveJavaScript(bool enabled)
        {
            ViewContext.UnobtrusiveJavaScriptEnabled = enabled;
        }

        public string Encode(string value)
        {
            return (!String.IsNullOrEmpty(value)) ? HttpUtility.HtmlEncode(value) : String.Empty;
        }

		public string Encode(object value)
		{
			return htmlEncoder(value);
		}

		// method used if HttpUtility.HtmlEncode(object) method does not exist
		private static string EncodeLegacy(object value)
		{
			var stringVal = Convert.ToString(value, CultureInfo.CurrentCulture);
			return !string.IsNullOrEmpty(stringVal) ? HttpUtility.HtmlEncode(stringVal) : String.Empty;
		}

		// selects the v3.5 (legacy) or v4 HTML encoder
		private static HtmlEncoder GetHtmlEncoder()
		{
			return TypeHelpers.CreateDelegate<HtmlEncoder>(TypeHelpers.SystemWebAssembly, "System.Web.HttpUtility", "HtmlEncode", null)
				?? EncodeLegacy;
		}

        internal string EvalString(string key)
        {
            return Convert.ToString(ViewData.Eval(key), CultureInfo.CurrentCulture);
        }

        internal string EvalString(string key, string format)
        {
            return Convert.ToString(ViewData.Eval(key, format), CultureInfo.CurrentCulture);
        }

        public string FormatValue(object value, string format)
        {
            return ViewDataDictionary.FormatValueInternal(value, format);
        }

        internal bool EvalBoolean(string key)
        {
            return Convert.ToBoolean(ViewData.Eval(key), CultureInfo.InvariantCulture);
        }

        public static string GenerateIdFromName(string name)
        {
            return GenerateIdFromName(name, TagBuilder.IdAttributeDotReplacement);
        }

        public static string GenerateIdFromName(string name, string idAttributeDotReplacement)
        {
            if (name == null) {
                throw new ArgumentNullException("name");
            }

            if (idAttributeDotReplacement == null) {
                throw new ArgumentNullException("idAttributeDotReplacement");
            }

            // TagBuilder.CreateSanitizedId returns null for empty strings, return String.Empty instead to avoid breaking change
            if (name.Length == 0) {
                return String.Empty;
            }

            return TagBuilder.CreateSanitizedId(name, idAttributeDotReplacement);
        }

        public static string GetFormMethodString(FormMethod method)
        {
            switch (method) {
                case FormMethod.Get:
                    return "get";
                case FormMethod.Post:
                    return "post";
                default:
                    return "post";
            }
        }

		public static string GetInputTypeString(InputType inputType)
		{
			switch (inputType)
			{
				case InputType.CheckBox:
					return "checkbox";
				case InputType.Hidden:
					return "hidden";
				case InputType.Password:
					return "password";
				case InputType.Radio:
					return "radio";
				case InputType.Text:
					return "text";
				default:
					return "text";
			}
		}

        internal object GetModelStateValue(string key, Type destinationType)
        {
            if (this.HttpRequest == null || this.HttpRequest.HttpMethod == HttpMethods.Get)
                return null;

            var postedValue = this.HttpRequest.FormData[key];
            if (postedValue == null)
                return null;

            if (destinationType == typeof (string))
                return postedValue;

            return new ValueProviderResult(postedValue, postedValue, null).ConvertTo(destinationType, null);
        }

        public IDictionary<string, object> GetUnobtrusiveValidationAttributes(string name)
        {
            return GetUnobtrusiveValidationAttributes(name, metadata: null);
        }

        // Only render attributes if unobtrusive client-side validation is enabled, and then only if we've
        // never rendered validation for a field with this name in this form. Also, if there's no form context,
        // then we can't render the attributes (we'd have no <form> to attach them to).
        public IDictionary<string, object> GetUnobtrusiveValidationAttributes(string name, ModelMetadata metadata)
        {
            Dictionary<string, object> results = new Dictionary<string, object>();

            //TODO: Awaiting implementation of ViewContext setter
            /*
            // The ordering of these 3 checks (and the early exits) is for performance reasons.
            if (!ViewContext.UnobtrusiveJavaScriptEnabled) {
                return results;
            }

            FormContext formContext = ViewContext.GetFormContextForClientValidation();
            if (formContext == null) {
                return results;
            }

            string fullName = ViewData.TemplateInfo.GetFullHtmlFieldName(name);
            if (formContext.RenderedField(fullName)) {
                return results;
            }

            formContext.RenderedField(fullName, true);

            IEnumerable<ModelClientValidationRule> clientRules = ClientValidationRuleFactory(name, metadata);
            UnobtrusiveValidationAttributesGenerator.GetValidationAttributes(clientRules, results);
            */
            return results;
        }

        public MvcHtmlString HttpMethodOverride(HttpVerbs httpVerb)
        {
            string httpMethod;
            switch (httpVerb) {
                case HttpVerbs.Delete:
                    httpMethod = "DELETE";
                    break;
                case HttpVerbs.Head:
                    httpMethod = "HEAD";
                    break;
                case HttpVerbs.Put:
                    httpMethod = "PUT";
                    break;
                case HttpVerbs.Patch:
                    httpMethod = "PATCH";
                    break;
                case HttpVerbs.Options:
                    httpMethod = "OPTIONS";
                    break;
                default:
                    throw new ArgumentException(MvcResources.HtmlHelper_InvalidHttpVerb, "httpVerb");
            }

            return HttpMethodOverride(httpMethod);
        }

		public MvcHtmlString HttpMethodOverride(string httpMethod)
		{
			if (String.IsNullOrEmpty(httpMethod))
			{
				throw new ArgumentException(MvcResources.Common_NullOrEmpty, "httpMethod");
			}
			if (String.Equals(httpMethod, "GET", StringComparison.OrdinalIgnoreCase) ||
				String.Equals(httpMethod, "POST", StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException(MvcResources.HtmlHelper_InvalidHttpMethod, "httpMethod");
			}

			TagBuilder tagBuilder = new TagBuilder("input");
			tagBuilder.Attributes["type"] = "hidden";
			tagBuilder.Attributes["name"] = HttpHeaders.XHttpMethodOverride;
			tagBuilder.Attributes["value"] = httpMethod;

			return tagBuilder.ToHtmlString(TagRenderMode.SelfClosing);
		}

        public MvcHtmlString Raw(object content)
		{
			if (content == null) return null;
			var strContent = content as string;
            return MvcHtmlString.Create(strContent ?? content.ToString()); //MvcHtmlString
		}

        public bool HasFieldError(string errorName)
        {
            return GetFieldError(errorName) != null;
        }

        public ResponseError GetFieldError(string errorName)
        {
            var errorStatus = this.GetErrorStatus();
            if (errorStatus == null || errorStatus.Errors == null) 
                return null;

            return errorStatus.Errors.FirstOrDefault(x => x.FieldName.EqualsIgnoreCase(errorName));
        }

        public ResponseStatus GetErrorStatus()
        {
            var errorStatus = this.HttpRequest.GetItem(HtmlFormat.ErrorStatusKey);
            return errorStatus as ResponseStatus;
        }

        public MvcHtmlString GetErrorMessage()
        {
            var errorStatus = GetErrorStatus();
            return errorStatus == null ? null : MvcHtmlString.Create(errorStatus.Message);
        }

        public MvcHtmlString RenderMarkdownToHtml(string markdown)
        {
            var feature = HostContext.GetPlugin<MarkdownFormat>();
            return feature != null
                ? MvcHtmlString.Create(feature.Transform(markdown))
                : new MvcHtmlString(new MarkdownSharp.Markdown().Transform(markdown));
        }
    }

	public static class HtmlHelperExtensions
	{
	    public static IRequest GetHttpRequest(this HtmlHelper html)
	    {
	        return html != null ? html.HttpRequest : null;
	    }
	}

}