namespace ServiceStack.WebHost.Endpoints.Tests.UseCases;

public class HelloJwt : IReturn<HelloJwtResponse>, IHasBearerToken
{
    public string Name { get; set; }
    public string BearerToken { get; set; }
}
public class HelloJwtResponse
{
    public string Result { get; set; }
}

[Authenticate]
public class JwtServices : Service
{
    public object Any(HelloJwt request)
    {
        return new HelloJwtResponse { Result = $"Hello, {request.Name}" };
    }
}