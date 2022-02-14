using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack;
using ServiceStack.Auth;

namespace ServiceStack.OpenApi.Tests.Services
{
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

#pragma warning disable CS0618
            this.SaveSession(session);
#pragma warning restore CS0618

            return new GetSessionResponse
            {
                Result = SessionAs<CustomUserSession>(),
                UnAuthInfo = unAuthInfo,
            };
        }
    }
}