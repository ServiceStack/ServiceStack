using System;
using System.Linq;
using Raven.Client.Indexes;
using ServiceStack.Auth;

namespace ServiceStack.Authentication.RavenDb
{
    public class ServiceStack_UserAuth_ByOAuthProvider : AbstractIndexCreationTask<UserAuthDetails, ServiceStack_UserAuth_ByOAuthProvider.Result>
    {
        public class Result
        {
            public string Provider { get; set; }
            public string UserId { get; set; }
            public int UserAuthId { get; set; }
            public DateTime ModifiedDate { get; set; }
        }

        public ServiceStack_UserAuth_ByOAuthProvider()
        {
            Map = oauthProviders => from oauthProvider in oauthProviders
                                    select new Result
                                    {
                                        Provider = oauthProvider.Provider,
                                        UserId = oauthProvider.UserId,
                                        ModifiedDate = oauthProvider.ModifiedDate,
                                        UserAuthId = oauthProvider.UserAuthId
                                    };
        }
    }
}
