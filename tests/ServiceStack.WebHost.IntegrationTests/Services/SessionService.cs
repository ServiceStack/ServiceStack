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

	public class SessionService : Service
	{
        public object Any(Session request)
		{
			var untyped = SessionBag["untyped"] as CustomSession ?? new CustomSession();			
			var typed = SessionBag.Get<CustomSession>("typed") ?? new CustomSession();

			untyped.Counter++;
			typed.Counter++;

			SessionBag["untyped"] = untyped;
			SessionBag.Set("typed", typed);

			var response = new SessionResponse {
				Typed = typed,
				UnTyped = untyped,
			};

			return response;
		}
	}
}