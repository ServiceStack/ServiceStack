using System;
using System.Collections.Generic;
using System.Dynamic;
using ServiceStack.Common;
using ServiceStack.Html;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.Razor2.Templating
{
    public partial class TemplateService
	{
		/// <summary>
		/// Runs and returns the template with the specified name.
		/// </summary>
		public IRazorTemplate ExecuteTemplate<T>(T model, string name, string defaultTemplatePath=null, 
            IHttpRequest httpReq = null, IHttpResponse httpRes = null)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("The named of the cached template is required.");

		    ITemplate instance = GetTemplate(name);
            if (instance == null)
                throw new ArgumentException("No compiled template exists with the specified name.");

		    using (instance as IDisposable)
		    {
                var razorTemplate = InitTemplate(model, instance, httpReq, httpRes);

		        instance.Execute();

                var template = httpReq.GetTemplate();
                if (!string.IsNullOrEmpty(template))
                    template = viewEngine.HasTemplate(template) ? template : null;

                if (template == null && !razorTemplate.Layout.IsNullOrEmpty())
                    template = razorTemplate.Layout.MapServerPath();

                if (template == null)
                    template = defaultTemplatePath;

                var layoutTemplate = GetTemplate(template ?? RazorFormat.DefaultTemplate);
                if (layoutTemplate != null)
                {
                    layoutTemplate.ChildTemplate = razorTemplate;
                    layoutTemplate.SetState(instance.HtmlHelper);
                    SetService(layoutTemplate, this);
                    SetModel(layoutTemplate, model);
                    layoutTemplate.Execute();

                    return layoutTemplate;
                }
                else if (defaultTemplatePath != null)
                {
                    throw new ArgumentException(
                        "No template exists with the specified Layout: " + defaultTemplatePath);
                }

                return razorTemplate;
            }
		}

        internal IRazorTemplate InitTemplate<T>(T model, ITemplate instance, IHttpRequest httpReq = null, IHttpResponse httpRes = null)
        {
            SetService(instance, this);
            SetModel(instance, model);
            TemplateBase.ViewBag = new ExpandoObject();

            var razorTemplate = (IRazorTemplate) instance;
            razorTemplate.Init(viewEngine, new ViewDataDictionary<T>(model), httpReq, httpRes);
            return razorTemplate;
        }

        readonly Dictionary<string, string> pagePathAndNames = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

		public void RegisterPage(string pagePath, string pageName)
		{
			pagePathAndNames[pagePath] = pageName;
		}

		public bool ContainsPagePath(string pagePath)
		{
			return pagePathAndNames.ContainsKey(pagePath);
		}
		
		public bool ContainsPageName(string pageName)
		{
			return pagePathAndNames.ContainsValue(pageName);
		}

		public ITemplate GetAndCheckTemplate(string name)
		{
            ViewPageRef viewPageRef;
            if (templateRefCache.TryGetValue(name, out viewPageRef))
                viewEngine.ReloadIfNeeeded(viewPageRef);

            ITemplate instance;
		    templateCache.TryGetValue(name, out instance);
		    
            return instance.CloneTemplate();
		}

		public IRazorTemplate GetTemplate(string name)
		{
		    var instance = GetAndCheckTemplate(name);
            if (instance == null)
			{
			    var view = viewEngine.GetView(name);
                if (view == null)
                {
                    if (name == RazorFormat.DefaultTemplate)
                        return null;

                    //Re-check after all templates have been compiled
                    viewEngine.EnsureAllCompiled();
                    if (templateCache.TryGetValue(name, out instance))
                        return instance.CloneTemplate() as IRazorTemplate;

                    view = viewEngine.GetView(name);                    
                    if (view == null)
                        throw new Exception("Could not find template " + name);
                }
                view.Compile(); //compiling adds to templateCache

			    templateCache.TryGetValue(name, out instance);
			}
            return instance as IRazorTemplate; //Cloned in GetAndCheckTemplate()
		}

        public IRazorTemplate RenderPartial<T>(T model, string name, HtmlHelper htmlHelper)
		{
			var template = GetTemplate(name);
            using (template as IDisposable)
            {
                SetService(template, this);
                SetModel(template, model);
                template.SetState(htmlHelper);

                //TODO: make less ugly, 
                //since executing templates clears the buffer we need to capture 
                //what's been rendered and prepend after.
                var capture = template.Result;

                try
                {
                    template.Execute();
                }
                catch (Exception ex)
                {
					throw new InvalidOperationException(
						"Could not execute partial: " + name + ", model: " + model + ", message: " + ex.Message);
                }

                template.Prepend(capture);

                return template;
            }
		}
	}
}