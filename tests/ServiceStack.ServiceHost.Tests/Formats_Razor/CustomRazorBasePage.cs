using ServiceStack.Razor2;
using ServiceStack.ServiceHost.Tests.AppData;

namespace ServiceStack.ServiceHost.Tests.Formats_Razor
{
	public abstract class CustomRazorBasePage<TModel> : ViewPage<TModel>
	{
		public FormatHelpers Fmt = new FormatHelpers();
		public NorthwindHelpers Nwnd = new NorthwindHelpers();
	}
}
