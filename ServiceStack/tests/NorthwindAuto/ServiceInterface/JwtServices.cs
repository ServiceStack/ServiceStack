using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace MyApp.ServiceInterface;

[ExcludeMetadata] // Exports AuthUserSession
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

[ValidateIsAuthenticated]
[Route("/secured")]
public class Secured : IReturn<SecuredResponse>
{
    public string Name { get; set; }
}

public class SecuredResponse
{
    public string Result { get; set; }

    public ResponseStatus ResponseStatus { get; set; }
}

[Route("/jwt-invalidate")]
public class InvalidateLastAccessToken : IReturn<EmptyResponse> {}

public class JwtServices : Service
{
    public object Any(Secured request) =>
        new SecuredResponse { Result = request.Name };
    
    public object Any(CreateJwt request)
    {
        var jwtAuthProvider = (JwtAuthProvider)AuthenticateService.GetRequiredJwtAuthProvider();

        if (request.JwtExpiry != null)
        {
            jwtAuthProvider.CreatePayloadFilter = (jwtPayload, session) =>
                jwtPayload["exp"] = request.JwtExpiry.Value.ToUnixTime().ToString();
        }

        var jwtSession = request.ConvertTo<AuthUserSession>();
        var token = jwtAuthProvider.CreateJwtBearerToken(jwtSession);

        jwtAuthProvider.CreatePayloadFilter = null;

        return new CreateJwtResponse
        {
            Token = token
        };
    }
    
    public object Any(CreateRefreshJwt request)
    {
        var jwtAuthProvider = (JwtAuthProvider)AuthenticateService.GetRequiredJwtAuthProvider();

        var jwtHeader = new JsonObject
        {
            {"typ", "JWTR"}, //RefreshToken
            {"alg", jwtAuthProvider.HashAlgorithm}
        };

        var keyId = jwtAuthProvider.GetKeyId(Request);
        if (keyId != null)
            jwtHeader["kid"] = keyId;

        var now = DateTime.UtcNow;
        var jwtPayload = new JsonObject
        {
            {"sub", request.UserAuthId ?? "1"},
            {"iat", now.ToUnixTime().ToString()},
            {"exp", (request.JwtExpiry ?? DateTime.UtcNow.AddDays(1)).ToUnixTime().ToString()},
        };

        if (jwtAuthProvider.Audience != null)
            jwtPayload["aud"] = jwtAuthProvider.Audience;

        var hashAlgorithm = jwtAuthProvider.GetHashAlgorithm();
        var refreshToken = JwtAuthProvider.CreateJwt(jwtHeader, jwtPayload, hashAlgorithm);

        return new CreateRefreshJwtResponse
        {
            Token = refreshToken
        };
    }

    public object Any(InvalidateLastAccessToken request)
    {
        var jwtAuthProvider = AuthenticateService.GetRequiredJwtAuthProvider();
        jwtAuthProvider.InvalidateJwtIds.Add(jwtAuthProvider.LastJwtId());
        return new EmptyResponse();
    }
}