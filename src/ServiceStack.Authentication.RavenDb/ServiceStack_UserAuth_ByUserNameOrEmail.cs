using System.Linq;
using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;
using ServiceStack.Auth;

namespace ServiceStack.Authentication.RavenDb
{
    public class ServiceStack_UserAuth_ByUserNameOrEmail : AbstractIndexCreationTask<UserAuth, ServiceStack_UserAuth_ByUserNameOrEmail.Result>
    {
        public class Result
        {
            public string UserName { get; set; }
            public string Email { get; set; }
            public string[] Search { get; set; }
        }

        public ServiceStack_UserAuth_ByUserNameOrEmail()
        {
            Map = users => from user in users
                           select new Result
                           {
                               UserName = user.UserName,
                               Email = user.Email,
                               Search = new[] { user.UserName, user.Email }
                           };

            Index(x => x.Search, FieldIndexing.Analyzed);
        }
    }
}
