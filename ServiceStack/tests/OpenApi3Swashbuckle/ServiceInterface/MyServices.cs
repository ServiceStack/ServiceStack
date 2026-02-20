using MyApp.ServiceModel;
using ServiceStack;

namespace MyApp.ServiceInterface;

public class MyServices : Service
{
    public object Any(Hello request) => new HelloResponse
    {
        Result = $"Hello, {request.Name ?? "World"}!"
    };
    
    public object Any(HelloSecure request) => new HelloResponse
    {
        Result = $"Hello, {request.Name ?? "World"}!"
    };

    public object Any(HelloApiKey request) => new HelloResponse
    {
        Result = $"Hello, {request.Name ?? "World"}!"
    };

    public object Any(HelloPost request) => new HelloResponse
    {
        Result = $"Hello, {request.Name ?? "World"}!"
    };
}

