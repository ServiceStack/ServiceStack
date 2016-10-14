using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack;
using ServiceStack.Auth;

namespace Mvc.Core.Tests
{
    public class HomeViewModel
    {
        public CustomUserSession Session { get; set; }
        public List<UserAuth> UserAuths { get; set; }
        public List<UserAuthDetails> UserAuthDetails { get; set; }
    }

    public class CustomUserSession : AuthUserSession
    {
        [DataMember]
        public string CustomName { get; set; }

        [DataMember]
        public string CustomInfo { get; set; }

        public override void OnAuthenticated(IServiceBase authService, IAuthSession session,
            IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            var unAuthInfo = authService.GetSessionBag().Get<UnAuthInfo>();

            if (unAuthInfo != null)
                this.CustomInfo = unAuthInfo.CustomInfo;
        }
    }

    public class UnAuthInfo
    {
        public string CustomInfo { get; set; }
    }

    [Route("/session")]
    public class GetSession : IReturn<GetSessionResponse>
    {
    }

    [Route("/session/edit/{CustomName}")]
    public class UpdateSession : IReturn<GetSessionResponse>
    {
        public string CustomName { get; set; }
    }

    public class GetSessionResponse
    {
        public CustomUserSession Result { get; set; }

        public UnAuthInfo UnAuthInfo { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    public class SessionService : Service
    {
        public object Any(GetSession request)
        {
            return new GetSessionResponse
            {
                Result = SessionAs<CustomUserSession>(),
                UnAuthInfo = SessionBag.Get<UnAuthInfo>(typeof(UnAuthInfo).Name),
            };
        }

        public object Any(UpdateSession request)
        {
            var session = SessionAs<CustomUserSession>();
            session.CustomName = request.CustomName;

            var unAuthInfo = SessionBag.Get<UnAuthInfo>() ?? new UnAuthInfo();
            unAuthInfo.CustomInfo = request.CustomName + " - CustomInfo";
            SessionBag.Set(unAuthInfo);

            this.SaveSession(session);

            return new GetSessionResponse
            {
                Result = SessionAs<CustomUserSession>(),
                UnAuthInfo = unAuthInfo,
            };
        }

        public static void ResetUsers(OrmLiteAuthRepository authRepo)
        {
            authRepo.DropAndReCreateTables();

            CreateUser(authRepo, 1, "test", "test", new List<string> { "TheRole" }, new List<string> { "ThePermission" });
            CreateUser(authRepo, 2, "test2", "test2");
        }

        private static void CreateUser(OrmLiteAuthRepository authRepo,
            int id, string username, string password, List<string> roles = null, List<string> permissions = null)
        {
            string hash;
            string salt;
            new SaltedHash().GetHashAndSaltString(password, out hash, out salt);

            authRepo.CreateUserAuth(new UserAuth
            {
                Id = id,
                DisplayName = username + " DisplayName",
                Email = username + "@gmail.com",
                UserName = username,
                FirstName = "First " + username,
                LastName = "Last " + username,
                PasswordHash = hash,
                Salt = salt,
                Roles = roles,
                Permissions = permissions
            }, password);

            authRepo.AssignRoles(id.ToString(), roles, permissions);
        }
    }
}