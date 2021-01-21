using Raven.Client.Documents.Indexes;
using System;
using System.Linq;

namespace ServiceStack.Authentication.RavenDb
{
    public class UserAuth_By_UserNameOrEmail : AbstractIndexCreationTask<RavenUserAuth, UserAuth_By_UserNameOrEmail.Result>
    {
        //ncrunch: no coverage start
        public class Result
        {
            public string UserName { get; set; }
            public string Email { get; set; }
            public string[] Search { get; set; }
        }
        //ncrunch: no coverage end

        public UserAuth_By_UserNameOrEmail()
        {
            Map = users => from user in users
                           select new Result
                           {
                               UserName = user.UserName,
                               Email = user.Email,
                               Search = new[] { user.UserName, user.Email }
                           };

            Index(x => x.Search, FieldIndexing.Exact);
        }
    }

    public class UserAuth_By_UserAuthDetails : AbstractIndexCreationTask<RavenUserAuthDetails, UserAuth_By_UserAuthDetails.Result>
    {
        //ncrunch: no coverage start
        public class Result
        {
            public string Provider { get; set; }
            public string UserId { get; set; }
            public string UserAuthId { get; set; }
            public DateTime ModifiedDate { get; set; }
        }
        //ncrunch: no coverage end

        public UserAuth_By_UserAuthDetails()
        {
            Map = userDetails => from userDetail in userDetails
                                 select new Result
                                 {
                                     Provider = userDetail.Provider,
                                     UserId = userDetail.UserId,
                                     ModifiedDate = userDetail.ModifiedDate,
                                     UserAuthId = userDetail.RefIdStr,
                                 };
        }
    }
}
