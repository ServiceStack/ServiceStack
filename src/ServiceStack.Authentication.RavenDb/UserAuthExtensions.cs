using ServiceStack.Auth;

namespace ServiceStack.Authentication.RavenDb
{
    public static class UserAuthExtensions
    {
        public static RavenUserAuth ToRavenUserAuth(this UserAuth ua)
        {
            var ra = ua.ConvertTo<RavenUserAuth>();
            if (ua.Id > 0)
                ra.Key = RavenIdConverter.ToString(RavenIdConverter.RavenUserAuthsIdPrefix, ua.Id);
            return ra;
        }
    }
}