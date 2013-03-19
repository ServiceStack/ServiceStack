/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. All rights reserved.
 *
 * This software is subject to the Microsoft Public License (Ms-PL). 
 * A copy of the license can be found in the license.htm file included 
 * in this distribution.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.Markdown;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Support.Markdown;

namespace ServiceStack.Html
{
	[Flags]
	public enum HttpVerbs
	{
		Get = 1 << 0,
		Post = 1 << 1,
		Put = 1 << 2,
		Delete = 1 << 3,
		Head = 1 << 4
	}

	public enum InputType
	{
		CheckBox,
		Hidden,
		Password,
		Radio,
		Text
	}

	public class HtmlHelper<TModel> : HtmlHelper
	{
		private ViewDataDictionary<TModel> viewData;
		public new ViewDataDictionary<TModel> ViewData
		{
			get 
            { 
                return base.ViewData as ViewDataDictionary<TModel> 
                    ?? new ViewDataDictionary<TModel>((TModel)base.ViewData.Model); 
            }
		}
	}

	public class HtmlHelper
	{
		public static List<Type> HtmlExtensions = new List<Type> 
		{
			typeof(DisplayTextExtensions),
			typeof(InputExtensions),
			typeof(LabelExtensions),
			typeof(TextAreaExtensions),
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

		public bool RenderHtml { get; protected set; }

        public IHttpRequest HttpRequest { get; set; }
        public IHttpResponse HttpResponse { get; set; }
        public IViewEngine ViewEngine { get; set; }

	    public MarkdownPage MarkdownPage { get; protected set; }
		public Dictionary<string, object> ScopeArgs { get; protected set; }
	    private ViewDataDictionary viewData;

	    public virtual ViewDataDictionary ViewData
	    {
	        get
	        {
	            return viewData ??
	                   (viewData = new ViewDataDictionary());
	        }
	        protected set { viewData = value; }
	    }

        public void SetState(HtmlHelper htmlHelper)
        {
            if (htmlHelper == null) return;

            HttpRequest = htmlHelper.HttpRequest;
            HttpResponse = htmlHelper.HttpResponse;
            ScopeArgs = htmlHelper.ScopeArgs;
            viewData = htmlHelper.ViewData;
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
                var result = ViewEngine.RenderPartial(viewName, model, this.RenderHtml, this);
                return MvcHtmlString.Create(result);
            }
            finally
            {
                this.viewData = masterModel;
            }
        }

        public string Debug(object model)
        {
            if (model != null)
            {
                model.PrintDump();
            }

            return null;
        }

        public MvcHtmlString Raw(object content)
		{
			if (content == null) return null;
			var strContent = content as string;
            return MvcHtmlString.Create(strContent ?? content.ToString()); //MvcHtmlString
		}

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

		public string AttributeEncode(string value)
		{
			return !string.IsNullOrEmpty(value) ? HttpUtility.HtmlAttributeEncode(value) : String.Empty;
		}

		public string AttributeEncode(object value)
		{
			return AttributeEncode(Convert.ToString(value, CultureInfo.InvariantCulture));
		}

		public string Encode(string value)
		{
			return !string.IsNullOrEmpty(value) ? HttpUtility.HtmlEncode(value) : String.Empty;
		}

		public string Encode(object value)
		{
			return htmlEncoder(value);
		}

		internal object GetModelStateValue(string key, Type destinationType)
		{
            ModelState modelState;
			if (ViewData.ModelState.TryGetValue(key, out modelState))
			{
				if (modelState.Value != null)
				{
					return modelState.Value.ConvertTo(destinationType, null /* culture */);
				}
			}
			return null;
		}

		internal string EvalString(string key)
		{
			return Convert.ToString(ViewData.Eval(key), CultureInfo.CurrentCulture);
		}

		internal bool EvalBoolean(string key)
		{
			return Convert.ToBoolean(ViewData.Eval(key), CultureInfo.InvariantCulture);
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

		public MvcHtmlString HttpMethodOverride(HttpVerbs httpVerb)
		{
			string httpMethod;
			switch (httpVerb)
			{
				case HttpVerbs.Delete:
					httpMethod = "DELETE";
					break;
				case HttpVerbs.Head:
					httpMethod = "HEAD";
					break;
				case HttpVerbs.Put:
					httpMethod = "PUT";
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

			var tagBuilder = new TagBuilder("input");
			tagBuilder.Attributes["type"] = "hidden";
			tagBuilder.Attributes["name"] = HttpHeaders.XHttpMethodOverride;
			tagBuilder.Attributes["value"] = httpMethod;

			return tagBuilder.ToMvcHtmlString(TagRenderMode.SelfClosing);
		}
	}

	public static class HtmlHelperExtensions
	{
	    public static IHttpRequest GetHttpRequest(this HtmlHelper html)
	    {
	        return html != null ? html.HttpRequest : null;
	    }
	}

}