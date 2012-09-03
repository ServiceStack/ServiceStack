using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceInterface.ServiceModel;

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
			this.OAuthProviders = new List<UserOAuthProvider>();
		}

		public List<UserAuth> Results { get; set; }

		public List<UserOAuthProvider> OAuthProviders { get; set; }

		public ResponseStatus ResponseStatus { get; set; }
	}

	//Implementation. Can be called via any endpoint or format, see: http://servicestack.net/ServiceStack.Hello/
	public class UserAuthsService : ServiceBase<UserAuths>
	{
		public IDbConnectionFactory DbFactory { get; set; }

		protected override object Run(UserAuths request)
		{
			return new UserAuthsResponse {
				Results = DbFactory.Run(db => db.Select<UserAuth>()),
				OAuthProviders = DbFactory.Run(db => db.Select<UserOAuthProvider>()),
			};
		}
	}
}