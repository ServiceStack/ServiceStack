using System.Collections.Generic;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Markdown
{
	public interface ITemplatePage
	{
		IAppHost AppHost { get; set; }
		T TryResolve<T>();
		Dictionary<string, object> ScopeArgs { get; set; }
	}
}