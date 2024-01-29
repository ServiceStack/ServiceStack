using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;

namespace MyApp.ServiceInterface;

[Route("/testauth")]
public class TestAuth : IReturn<TestAuthResponse> { }

public class TestAuthResponse
{
    public string UserId { get; set; }
    public string SessionId { get; set; }
    public string UserName { get; set; }
    public string DisplayName { get; set; }

    public ResponseStatus ResponseStatus { get; set; }
}

public class HelloAuth : IReturn<HelloResponse>
{
    public string Name { get; set; }
}

[Authenticate]
public class TestAuthService : Service
{
    public object Any(HelloAuth request) => new HelloResponse { Result = $"Hello, {request.Name}!"};

    public object Any(TestAuth request)
    {
        var session = base.SessionAs<CustomUserSession>();
        return new TestAuthResponse
        {
            UserId = session.UserAuthId,
            UserName = session.UserAuthName ?? session.UserName,
            DisplayName = session.DisplayName
                          ?? session.UserName
                          ?? "{0} {1}".Fmt(session.FirstName, session.LastName).Trim(),
            SessionId = session.Id,
        };
    }
}

public class RequiresAdmin : IReturn<RequiresAdmin>
{
    public int Id { get; set; }
}

[RequiredRole("Admin")]
public class AdminServices : Service
{
    public object Any(RequiresAdmin request) => request;
}