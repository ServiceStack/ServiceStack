using System.Collections.Generic;
using ServiceStack.Auth;
using ServiceStack.OrmLite;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[Route("/userauths")]
	public class UserAuths
	{
		public int[] Ids { get; set; }
	}

	public class UserAuthsResponse : IHasResponseStatus
	{
		public UserAuthsResponse()
		{
			this.Results = new List<UserAuth>();
			this.OAuthProviders = new List<UserAuthDetails>();
		}

		public List<UserAuth> Results { get; set; }

		public List<UserAuthDetails> OAuthProviders { get; set; }

		public ResponseStatus ResponseStatus { get; set; }
	}

	//Implementation. Can be called via any endpoint or format, see: http://servicestack.net/ServiceStack.Hello/
	public class UserAuthsService : Service
	{
        public object Any(UserAuths request)
		{
			return new UserAuthsResponse {
				Results = Db.Select<UserAuth>(),
				OAuthProviders = Db.Select<UserAuthDetails>(),
			};
		}
	}
}