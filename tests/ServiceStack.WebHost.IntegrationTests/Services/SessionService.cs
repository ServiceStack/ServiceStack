using System.Runtime.Serialization;
using System.Web;
using ServiceStack.CacheAccess;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	public class CustomSession
	{
		public int Counter { get; set; }
	}

	[Route("/session")]
	public class Session
	{
		public string Value { get; set; }
	}

	public class SessionResponse
	{
		public CustomSession Typed { get; set; }
		public CustomSession UnTyped { get; set; }
	}

	public class SessionService : ServiceInterface.Service
	{
        public object Any(Session request)
		{
			var untyped = Session["untyped"] as CustomSession ?? new CustomSession();			
			var typed = Session.Get<CustomSession>("typed") ?? new CustomSession();

			untyped.Counter++;
			typed.Counter++;

			Session["untyped"] = untyped;
			Session.Set("typed", typed);

			var response = new SessionResponse {
				Typed = typed,
				UnTyped = untyped,
			};

			return response;
		}
	}
}