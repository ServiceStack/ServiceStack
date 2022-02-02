using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Text;

namespace Check.ServiceInterface
{
    [Route("/jwt")]
    public class CreateJwt : AuthUserSession, IReturn<CreateJwtResponse>
    {
        public DateTime? JwtExpiry { get; set; }
    }

    public class CreateJwtResponse
    {
        public string Token { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/jwt-refresh")]
    public class CreateRefreshJwt : IReturn<CreateRefreshJwtResponse>
    {
        public string UserAuthId { get; set; }
        public DateTime? JwtExpiry { get; set; }
    }

    public class CreateRefreshJwtResponse
    {
        public string Token { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    public class JwtServices : Service
    {
        public object Any(CreateJwt request)
        {
            var jwtProvider = (JwtAuthProvider)AuthenticateService.GetAuthProvider(JwtAuthProvider.Name);

            if (request.JwtExpiry != null)
            {
                jwtProvider.CreatePayloadFilter = (jwtPayload, session) =>
                    jwtPayload["exp"] = request.JwtExpiry.Value.ToUnixTime().ToString();
            }

            var jwtSession = request.ConvertTo<AuthUserSession>();
            var token = jwtProvider.CreateJwtBearerToken(jwtSession);

            jwtProvider.CreatePayloadFilter = null;

            return new CreateJwtResponse
            {
                Token = token
            };
        }

        public object Any(CreateRefreshJwt request)
        {
            var jwtProvider = (JwtAuthProvider)AuthenticateService.GetAuthProvider(JwtAuthProvider.Name);

            var jwtHeader = new JsonObject
            {
                {"typ", "JWTR"}, //RefreshToken
                {"alg", jwtProvider.HashAlgorithm}
            };

            var keyId = jwtProvider.GetKeyId(Request);
            if (keyId != null)
                jwtHeader["kid"] = keyId;

            var now = DateTime.UtcNow;
            var jwtPayload = new JsonObject
            {
                {"sub", request.UserAuthId ?? "1"},
                {"iat", now.ToUnixTime().ToString()},
                {"exp", (request.JwtExpiry ?? DateTime.UtcNow.AddDays(1)).ToUnixTime().ToString()},
            };

            if (jwtProvider.Audience != null)
                jwtPayload["aud"] = jwtProvider.Audience;

            var hashAlgoritm = jwtProvider.GetHashAlgorithm();
            var refreshToken = JwtAuthProvider.CreateJwt(jwtHeader, jwtPayload, hashAlgoritm);

            return new CreateRefreshJwtResponse
            {
                Token = refreshToken
            };
        }
    }
}
