using ServiceStack;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface;

public class MyServices : Service
{
    public async Task<object> Any(Hello request)
    {
        var authSession = await this.GetSessionAsync();
        return new HelloResponse { Result = $"Hello, {request.Name}! You are authenticated as {authSession.UserName}" };
    }
}
