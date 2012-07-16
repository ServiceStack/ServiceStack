using System.Collections.Generic;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Html
{
	public interface ITemplatePage
	{
		IAppHost AppHost { get; set; }
		T TryResolve<T>();
		Dictionary<string, object> ScopeArgs { get; set; }
	}
}