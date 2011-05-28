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
using ServiceStack.Markdown.Html;
using ServiceStack.WebHost.EndPoints.Formats;

namespace ServiceStack.Markdown
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
		public new ViewDataDictionary<TModel> ViewData { get; set; }
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
		private static readonly HtmlEncoder _htmlEncoder = GetHtmlEncoder();

		//some mockable love...
		private MarkdownFormat markdown;
		public MarkdownFormat Markdown
		{
			get { return markdown ?? MarkdownFormat.Instance; }
			set { markdown = value; }
		}

		public static HtmlHelper Instance = new HtmlHelper();

		public virtual ViewDataDictionary ViewData { get; set; }

		public string Partial(string viewName, object model)
		{
			var result = Markdown.RenderDynamicPage(viewName, model);
			return result;
		}

		public string Raw(string content)
		{
			return content;
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
			return (!String.IsNullOrEmpty(value)) ? HttpUtility.HtmlAttributeEncode(value) : String.Empty;
		}

		public string AttributeEncode(object value)
		{
			return AttributeEncode(Convert.ToString(value, CultureInfo.InvariantCulture));
		}

		public string Encode(string value)
		{
			return (!String.IsNullOrEmpty(value)) ? HttpUtility.HtmlEncode(value) : String.Empty;
		}

		public string Encode(object value)
		{
			return _htmlEncoder(value);
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
			return (!String.IsNullOrEmpty(stringVal)) ? HttpUtility.HtmlEncode(stringVal) : String.Empty;
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
			tagBuilder.Attributes["name"] = HttpRequestExtensions.XHttpMethodOverrideKey;
			tagBuilder.Attributes["value"] = httpMethod;

			return tagBuilder.ToMvcHtmlString(TagRenderMode.SelfClosing);
		}
	}

}