using System.ComponentModel;
using System.Web.Services;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests
{
	/// <summary>
	/// Summary description for Soap11WebService
	/// </summary>
	[WebService(Namespace = "http://schemas.servicestack.net/types")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[ToolboxItem(false)]
	// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
	// [System.Web.Script.Services.ScriptService]
	public class Soap11WebService : System.Web.Services.WebService
	{

		[WebMethod]
		public string HelloWorld()
		{
			return "Hello World";
		}

		[WebMethod]
		public ReverseResponse Reverse(Reverse request)
		{
			return new ReverseService().Any(request) as ReverseResponse;
		}
	}
}
