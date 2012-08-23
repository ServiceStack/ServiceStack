using System.Collections.Generic;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Html
{
	public interface ITemplatePage
	{
        IViewEngine ViewEngine { get; set; }
        IAppHost AppHost { get; set; }
		T Get<T>();
		Dictionary<string, object> ScopeArgs { get; set; }
	}
}