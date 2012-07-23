using System;
using System.Dynamic;
using ServiceStack.Common;
using ServiceStack.Html;
using ServiceStack.ServiceHost;

namespace ServiceStack.Razor.Templating
{
    public partial class TemplateService
	{
		/// <summary>
		/// Runs and returns the template with the specified name.
		/// </summary>
		public IRazorTemplate ExecuteTemplate<T>(T model, string name, string templatePath=null, 
            IHttpRequest httpReq = null, IHttpResponse httpRes = null)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("The named of the cached template is required.");

			ITemplate instance;
			if (!templateCache.TryGetValue(name, out instance))
				throw new ArgumentException("No compiled template exists with the specified name.");

			SetService(instance, this);
			SetModel(instance, model);
            TemplateBase.ViewBag = new ExpandoObject();

			var razorTemplate = (IRazorTemplate)instance;
            razorTemplate.Init(viewEngine, new ViewDataDictionary<T>(model), httpReq, httpRes);
	
			instance.Execute(); 

			if (!razorTemplate.Layout.IsNullOrEmpty())
				templatePath = razorTemplate.Layout.MapServerPath();

			if (templatePath != null)
			{
				var layoutTemplate = GetTemplate(templatePath);
				if (layoutTemplate == null)
					throw new ArgumentException(
						"No template exists with the specified Layout: " + templatePath);

				layoutTemplate.ChildTemplate = razorTemplate;
				SetService(layoutTemplate, this);
				SetModel(layoutTemplate, model);
				layoutTemplate.Execute();

				return layoutTemplate;
			}

			return razorTemplate;
		}

		public IRazorTemplate GetTemplate(string name)
		{
			ITemplate instance;
			templateCache.TryGetValue(name, out instance);
			return instance as IRazorTemplate;
		}

		public IRazorTemplate RenderPartial<T>(T model, string name)
		{
			var template = GetTemplate(name);
			SetService(template, this);
			SetModel(template, model);

			//TODO: make less ugly, 
			//since executing templates clears the buffer we need to capture 
			//what's been rendered and prepend after.
			var capture = template.Result;
			template.Execute();
			template.Prepend(capture);
			
			return template;
		}

	}
}