using System;
using System.Collections.Generic;
using RazorEngine.Templating;
using ServiceStack.WebHost.Endpoints;

namespace RazorEngine
{
	public abstract class RazorPageBase<TModel> : TemplateBase<TModel>, IRazorTemplate
	{
		public IAppHost AppHost { get; set; }

		public Dictionary<string, object> ScopeArgs { get; set; }

		public string Layout { get; set; }
		
		protected RazorPageBase()
		{
			this.ScopeArgs = new Dictionary<string, object>();
		}

		public T TryResolve<T>()
		{
			return this.AppHost.TryResolve<T>();
		}
	}
}